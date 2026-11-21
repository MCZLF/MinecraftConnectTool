using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MinecraftConnectTool.ExtensionUI;

namespace MinecraftConnectTool.Services;

internal sealed class ModCheckSession(CoreMode mode)
{
    internal const string FullReportSettingKey = "ShowFullModCheckReport";
    private const string OwnerHeader = "X-Mod-Check-Owner";
    private const string StatusHeader = "X-Mod-Check-Status";
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(5),
        MaxResponseContentBufferSize = 4 * 1024 * 1024
    };
    private readonly object _gate = new();
    private HostRun? _host;
    private Uri? _cancelledJoinAddress;

    public ModCheckProgress Progress { get; } = new();

    private sealed class HostRun(CancellationToken revocation)
    {
        public CancellationTokenSource Stop { get; } = CancellationTokenSource.CreateLinkedTokenSource(revocation);
        public TaskCompletionSource<string> Code { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void BeginHost(Action<string> log)
    {
        if (!ModCheckConsent.TryGetAuthorization(out var revocation))
            return;

        var run = new HostRun(revocation);
        lock (_gate)
        {
            _host?.Stop.Cancel();
            _host = run;
        }
        _ = PublishAsync(run, log);
    }

    public void SetHostCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;
        lock (_gate)
            _host?.Code.TrySetResult(code);
    }

    public void EndHost()
    {
        lock (_gate)
        {
            _host?.Stop.Cancel();
            _host = null;
        }
    }

    private async Task PublishAsync(HostRun run, Action<string> log)
    {
        var cancellationToken = run.Stop.Token;
        var owner = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var operation = Progress.Begin("模组校验 · 正在定位 Minecraft 实例");
        Uri? address = null;
        try
        {
            using var preparation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var announce = AnnouncePreparationAsync(run.Code.Task, owner, preparation.Token);
            ModManifest? manifest;
            try
            {
                manifest = await MinecraftModScanner.ScanRunningInstanceAsync(cancellationToken, allowServer: true,
                    progress: (done, total) => Progress.Scanning(operation, done, total));
            }
            finally
            {
                preparation.Cancel();
                address = await announce;
            }

            if (manifest is null)
            {
                log("[模组校验] 未能定位唯一的运行中 Minecraft 实例，跳过清单发布。");
                return;
            }

            if (manifest.ServerRelease is { } server)
            {
                var build = server.DisplayBuild ?? "构建未知";
                log($"[模组校验] 已识别独立服务端：Minecraft {manifest.GameVersion ?? "未知"}，{build}。");
            }

            address = GetAddress(await run.Code.Task.WaitAsync(cancellationToken));
            Progress.Update(operation, "模组校验 · 正在发布房主清单");
            var hasPublished = false;
            var retrying = false;
            var initialRoomRetries = 3;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ModCheckConsent.IsAllowed)
                    break;
                var retryDelay = TimeSpan.FromSeconds(20);
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Put, address)
                    {
                        Content = JsonContent.Create(manifest)
                    };
                    request.Headers.Add(OwnerHeader, owner);
                    // Let a sent PUT finish before DELETE so a late write cannot undo the cleanup.
                    using var response = await Http.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        if (!hasPublished)
                            log($"[模组校验] 已发布房主清单，共 {manifest.Mods.Length} 个模组。");
                        else if (retrying)
                            log($"[模组校验] 房主清单重新发布成功，共 {manifest.Mods.Length} 个模组。");

                        hasPublished = true;
                        retrying = false;
                        Progress.End(operation);
                    }
                    else
                    {
                        var statusCode = (int)response.StatusCode;
                        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge)
                        {
                            log($"[模组校验] 房主清单发布失败（HTTP {statusCode}），停止重试。");
                            break;
                        }
                        if (!retrying)
                            log($"[模组校验] 房主清单发布失败（HTTP {statusCode}），稍后重试。");

                        retrying = true;
                        if (response.StatusCode == HttpStatusCode.NotFound && initialRoomRetries-- > 0)
                            retryDelay = TimeSpan.FromSeconds(1);
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
                {
                    if (!retrying)
                        log("[模组校验] 暂时无法连接清单服务，稍后重试。");
                    retrying = true;
                }
                await Task.Delay(retrying ? retryDelay : TimeSpan.FromMinutes(5), cancellationToken);
            } while (true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException
            or InvalidOperationException or ArgumentException or NotSupportedException)
        {
            log("[模组校验] 无法读取或发布房主清单。");
        }
        finally
        {
            Progress.End(operation);
            if (address != null)
                await RemoveAsync(address, owner);
            lock (_gate)
            {
                if (ReferenceEquals(_host, run))
                    _host = null;
                run.Stop.Dispose();
            }
        }
    }

    private async Task<Uri?> AnnouncePreparationAsync(Task<string> code, string owner, CancellationToken token)
    {
        Uri? address = null;
        try
        {
            address = GetAddress(await code.WaitAsync(token));
            var prepareAddress = new Uri(address.AbsoluteUri + "/prepare");
            while (!token.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(2);
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, prepareAddress);
                    request.Headers.Add(OwnerHeader, owner);
                    // Finish a sent request before publishing or deleting the entry.
                    using var response = await Http.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                        delay = TimeSpan.FromSeconds(30);
                    else if (response.StatusCode != HttpStatusCode.NotFound)
                        break;
                }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
                {
                }
                await Task.Delay(delay, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
            // The publishing path reports invalid addresses.
        }
        return address;
    }

    public async Task<bool> CheckJoinAsync(string code, Action<string> log)
    {
        if (!ModCheckConsent.TryGetAuthorization(out var revocation))
        {
            _cancelledJoinAddress = null;
            return true;
        }

        var retry = false;
        var operation = Progress.Begin("模组校验 · 正在检查房主清单");
        try
        {
            var address = GetAddress(code);
            retry = address == _cancelledJoinAddress;
            log(retry ? "[模组校验] 正在重新校验模组和版本。" : "[模组校验] 正在检查房主清单是否可用。");
            if (!await IsHostAvailableAsync(address, revocation, () =>
                {
                    Progress.Update(operation, "模组校验 · 房主清单准备中");
                    log("[模组校验] 房主清单准备中，最多等待 30 秒。");
                }))
                return FinishWithoutComparison("房主清单暂不可用");

            Progress.Update(operation, "模组校验 · 正在定位 Minecraft 实例");
            var local = await MinecraftModScanner.ScanRunningInstanceAsync(revocation,
                progress: (done, total) => Progress.Scanning(operation, done, total));
            if (local is null)
                return FinishWithoutComparison("未能定位唯一的运行中 Minecraft 实例");

            revocation.ThrowIfCancellationRequested();
            if (!ModCheckConsent.IsAllowed)
                return true;

            Progress.Update(operation, "模组校验 · 正在提交清单并比对");
            log("[模组校验] 正在提交清单，由服务器比对。");
            var compareAddress = new Uri(address.AbsoluteUri + "/compare");
            using var response = await Http.PostAsJsonAsync(compareAddress, local, revocation);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return FinishWithoutComparison("房主清单已不可用");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ComparisonResult>(cancellationToken: revocation);
            if (result is null || !result.IsComplete)
                throw new IOException("Incomplete mod comparison result.");
            revocation.ThrowIfCancellationRequested();

            log("[模组校验] " + result.Summary + " " + result.VersionSummary);
            Progress.End(operation);
            var showFullReport = ConfigService.Read(FullReportSettingKey, false);
            if (showFullReport || result.HasHostDifferences || retry)
            {
                if (GetOwner() is not { } window)
                    return FinishWithoutComparison("无法显示模组比对结果");

                var title = result.HasDifferences ? "模组或版本有差异" : "模组比对结果";
                var sections = result.CreateReportSections(showFullReport);
                if (!await MD3MessageDialog.ShowJoinReportAsync(window, title,
                        showFullReport || sections.Count == 0 ? result.Summary : "", sections, warning: result.HasDifferences))
                {
                    _cancelledJoinAddress = address;
                    log("[模组校验] 已取消加入房间。");
                    return false;
                }
            }

            _cancelledJoinAddress = null;
        }
        catch (OperationCanceledException) when (revocation.IsCancellationRequested)
        {
            _cancelledJoinAddress = null;
            log("[模组校验] 上传授权已撤销，继续加入房间。");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException
            or OperationCanceledException or System.Text.Json.JsonException or InvalidOperationException
            or ArgumentException or NotSupportedException)
        {
            return FinishWithoutComparison("无法完成本次校验");
        }
        finally
        {
            Progress.End(operation);
        }
        return true;

        bool FinishWithoutComparison(string reason)
        {
            log($"[模组校验] {reason}，{(retry ? "本次未加入房间，请重试" : "本次跳过校验")}。");
            return !retry;
        }
    }

    private static async Task<bool> IsHostAvailableAsync(Uri address, CancellationToken revocation, Action onPreparing)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(revocation);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var preparing = false;
        var missingAttempts = 0;
        try
        {
            while (ModCheckConsent.IsAllowed)
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, address);
                using var response = await Http.SendAsync(request, timeout.Token);
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return true;
                if (response.StatusCode != HttpStatusCode.NotFound)
                    return false;

                var isPreparing = response.Headers.TryGetValues(StatusHeader, out var values)
                    && values.Contains("preparing", StringComparer.OrdinalIgnoreCase);
                if (isPreparing && !preparing)
                {
                    preparing = true;
                    onPreparing();
                }
                if (!isPreparing && (preparing || ++missingAttempts == 4))
                    return false;
                await Task.Delay(TimeSpan.FromSeconds(isPreparing ? 2 : 1), timeout.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !revocation.IsCancellationRequested)
        {
        }
        return false;
    }

    private static async Task RemoveAsync(Uri address, string owner)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, address);
            request.Headers.Add(OwnerHeader, owner);
            using var response = await Http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // The server expires the manifest if the stop notification cannot be sent.
        }
    }

    private Uri GetAddress(string code)
    {
        var normalizedCode = code.Trim();
        if (mode == CoreMode.ET && ScfLobbyCodeGenerator.TryParse(normalizedCode, out var lobby) && lobby != null)
            normalizedCode = $"{lobby.Identifier.Name}/{lobby.Identifier.Secret}";
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedCode)));
        var baseUrl = ServerBaseUrl;
        var route = $"{baseUrl.TrimEnd('/')}/api/mod-check/{mode.ToString().ToLowerInvariant()}/{key}";
        if (!Uri.TryCreate(route, UriKind.Absolute, out var address)
            || address.Scheme is not ("http" or "https") || address.Query.Length != 0 || address.Fragment.Length != 0)
            throw new IOException("Invalid mod check server address.");
        return address;
    }

    internal static string ServerBaseUrl =>
        Environment.GetEnvironmentVariable("MCT_MOD_CHECK_URL") ?? PlayerListService.ServerBaseUrl;

    private static Window? GetOwner() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    private sealed class ComparisonResult
    {
        public required int HostCount { get; init; }
        public required int GuestCount { get; init; }
        public required VersionCheck[] Versions { get; init; }
        public required ModFile[] Matched { get; init; }
        public required ModFile[] Missing { get; init; }
        public required ModFile[] Extra { get; init; }
        public required ChangedMod[] Changed { get; init; }

        public bool IsComplete => Versions is not null && Matched is not null && Missing is not null && Extra is not null && Changed is not null
            && Array.TrueForAll(Versions, version => version is not null)
            && Array.TrueForAll(Matched, file => file is not null)
            && Array.TrueForAll(Missing, file => file is not null)
            && Array.TrueForAll(Extra, file => file is not null)
            && Array.TrueForAll(Changed, file => file is not null);
        public bool HasHostDifferences => Missing.Length + Changed.Length != 0
            || Versions.Any(version => version.Status == "different");
        public bool HasDifferences => HasHostDifferences || Extra.Length != 0;
        public string Summary => $"房主 {HostCount} 个，加入方 {GuestCount} 个；一致 {Matched.Length} 个，"
            + $"加入方缺少 {Missing.Length} 个、多出 {Extra.Length} 个，文件不一致 {Changed.Length} 个。";
        public string VersionSummary => string.Join(" | ", Versions.Where(version => version.IsRelevant).Select(version => version.Describe()));

        public IReadOnlyList<MD3MessageSection> CreateReportSections(bool full)
        {
            var sections = new List<MD3MessageSection>();
            AddSection("缺少", FileRows(Missing));

            var mismatches = Changed.Select(mod => mod.ToReportRow(full));
            var versionRows = Versions.Where(version => full ? version.IsRelevant : version.Status == "different")
                .Select(version => new MD3MessageRow(version.Describe()));
            AddSection("不一致", full ? mismatches : mismatches.Concat(versionRows));

            if (full)
            {
                AddSection("加入方多出", FileRows(Extra));
                AddSection("版本比对", versionRows);
                AddSection("MD5 一致", FileRows(Matched));
            }
            return sections;

            IEnumerable<MD3MessageRow> FileRows(ModFile[] files) =>
                files.Select(mod => new MD3MessageRow(mod.FileName, full ? $"MD5  {mod.Md5}" : null));

            void AddSection(string label, IEnumerable<MD3MessageRow> rows)
            {
                var items = rows.ToArray();
                if (items.Length != 0)
                    sections.Add(new MD3MessageSection($"{label}（{items.Length}）", items));
            }
        }
    }

    private sealed record VersionCheck(string Field, string? Host, string? Guest, string Status)
    {
        public bool IsRelevant => Field is "minecraft" or "loader" || Host != null || Guest != null;

        public string Describe()
        {
            var label = Field switch
            {
                "minecraft" => "Minecraft",
                "loader" => "加载器",
                "loaderVersion" => "加载器版本",
                "fml" => "FML",
                "mcp" => "MCP",
                "neoForm" => "NeoForm",
                "java" => "Java",
                _ => Field
            };
            var state = Status switch
            {
                "different" => "不同",
                "match" => "一致",
                _ => "未能校对"
            };
            return $"{label}：房主 {Display(Host)}，加入方 {Display(Guest)}（{state}）";
        }

        private static string Display(string? value) => value switch
        {
            "vanilla" => "原版",
            "forge" => "Forge",
            "neoforge" => "NeoForge",
            "fabric" => "Fabric",
            "quilt" => "Quilt",
            _ => value ?? "未知"
        };
    }

    private sealed record ChangedMod(string FileName, string HostMd5, string? GuestMd5, string? GuestFileName)
    {
        public MD3MessageRow ToReportRow(bool full)
        {
            var details = GuestMd5 is null ? "加入方未匹配到对应的加载器 API"
                : GuestFileName is null ? null : $"加入方：{GuestFileName}";
            if (full)
            {
                var hashes = $"房主 MD5  {HostMd5}";
                if (GuestMd5 is not null)
                    hashes += $"\n加入方 MD5  {GuestMd5}";
                details = details is null ? hashes : $"{details}\n{hashes}";
            }
            return new MD3MessageRow(FileName, details);
        }
    }
}
