using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftConnectTool.Services;

// ============ 模型定义 ============

// ============ 公共节点 API 模型 ============

internal record PublicNodeDto
{
    [JsonPropertyName("success")] public bool IsSuccess { get; init; }
    [JsonPropertyName("data")] public PublicNodeDataDto? Data { get; init; }
}

internal record PublicNodeDataDto
{
    [JsonPropertyName("items")] public IReadOnlyList<PublicNodeItemDto>? Items { get; init; }
}

internal record PublicNodeItemDto
{
    [JsonPropertyName("address")] public string Host { get; init; } = "";
    [JsonPropertyName("allow_relay")] public bool IsAllowRelay { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

// ============ ruixuan.online 监控 API 模型 ============

internal record RuixuanStatusDto
{
    [JsonPropertyName("publicGroupList")] public IReadOnlyList<RuixuanGroupDto>? PublicGroupList { get; init; }
}

internal record RuixuanGroupDto
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("monitorList")] public IReadOnlyList<RuixuanMonitorDto>? MonitorList { get; init; }
}

internal record RuixuanMonitorDto
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("type")] public string? Type { get; init; }
}

public enum ETCoreState { Stopped, Running, Ready }

public enum ETConnectionType { Local, P2P, Relay, Unknown }

public enum PlayerRole { HOST, GUEST }

public class ETPlayerInfo
{
    public bool IsHost { get; init; }
    public string Hostname { get; init; } = "";
    public string? Username { get; init; }
    public string? VirtualIp { get; init; }
    public ETConnectionType ConnectionType { get; init; }
    public double Ping { get; set; }
    public double Loss { get; init; }
    public string? NatType { get; init; }
    public string? Vendor { get; init; }
}

public record ETCliPeerInfo
{
    [JsonPropertyName("hostname")] public string Hostname { get; init; } = "";
    [JsonPropertyName("ipv4")] public string Ipv4 { get; init; } = "";
    [JsonPropertyName("cost")] public string Cost { get; init; } = "";
    [JsonPropertyName("lat_ms")] public string LatMs { get; init; } = "";
    [JsonPropertyName("loss_rate")] public string LossRate { get; init; } = "";
    [JsonPropertyName("nat_type")] public string NatType { get; init; } = "";
    [JsonPropertyName("version")] public string Version { get; init; } = "";
}

// ============ Scaffolding 协议模型 ============

public class ScfPlayerProfile
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("machine_id")] public string MachineId { get; set; } = "";
    [JsonPropertyName("vendor")] public string Vendor { get; set; } = "";
    [JsonPropertyName("kind")] public PlayerRole? Kind { get; set; }
}

public class ScfLobbyInfo
{
    public string FullCode { get; init; } = "";
    public NetworkIdentifier Identifier { get; init; } = null!;
}

public class NetworkIdentifier
{
    public string Name { get; init; } = "";
    public string Secret { get; init; } = "";
}

// ============ Scaffolding 协议读写器 ============

internal static class ScfProtocolWriter
{
    public static async Task WriteRequestAsync(PipeWriter writer, string requestType, ReadOnlyMemory<byte> body, CancellationToken ct = default)
    {
        var typeBytes = Encoding.ASCII.GetBytes(requestType);
        // 使用 byte[] 代替 Span 避免 CS9202
        var headerLen = 1 + typeBytes.Length + 4;
        var header = new byte[headerLen];
        header[0] = (byte)typeBytes.Length;
        Buffer.BlockCopy(typeBytes, 0, header, 1, typeBytes.Length);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1 + typeBytes.Length), (uint)body.Length);
        await writer.WriteAsync(header, ct);
        if (!body.IsEmpty) await writer.WriteAsync(body, ct);
        var result = await writer.FlushAsync(ct);
        if (result.IsCanceled) throw new OperationCanceledException();
    }
}

internal static class ScfProtocolReader
{
    public static async Task<(byte Status, byte[] Body)> ReadResponseAsync(PipeReader reader, CancellationToken ct = default)
    {
        while (true)
        {
            var result = await reader.ReadAsync(ct);
            var buffer = result.Buffer;
            if (buffer.Length < 5)
            {
                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted) throw new InvalidOperationException("Connection closed");
                continue;
            }
            // 使用 ToArray 代替 stackalloc 避免 CS9202
            var header = buffer.Slice(0, 5).ToArray();
            var status = header[0];
            var bodyLen = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(1));
            if (buffer.Length < 5 + bodyLen)
            {
                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted) throw new InvalidOperationException("Connection closed");
                continue;
            }
            var body = buffer.Slice(5, bodyLen).ToArray();
            reader.AdvanceTo(buffer.Slice(5 + bodyLen).Start);
            return (status, body);
        }
    }
}

// ============ Scaffolding 房间码生成器 ============

internal static class ScfLobbyCodeGenerator
{
    private const string Chars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int BaseVal = 34;
    private const int DataLength = 16;

    /// <summary>
    /// 生成符合 Scaffolding 协议的提示码
    /// </summary>
    public static ScfLobbyInfo Generate()
    {
        while (true)
        {
            var codeChars = new char[DataLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < DataLength; i++)
                {
                    byte[] b = new byte[4];
                    rng.GetBytes(b);
                    codeChars[i] = Chars[Math.Abs(BitConverter.ToInt32(b, 0)) % BaseVal];
                }
            }

            // 校验: 依小端序读得的整数能被7整除
            long checkValue = 0;
            for (int i = DataLength - 1; i >= 0; i--)
                checkValue = checkValue * BaseVal + MapToValue(codeChars[i]);
            if (checkValue % 7 != 0) continue;

            var payload = $"{codeChars[0]}{codeChars[1]}{codeChars[2]}{codeChars[3]}-" +
                         $"{codeChars[4]}{codeChars[5]}{codeChars[6]}{codeChars[7]}-" +
                         $"{codeChars[8]}{codeChars[9]}{codeChars[10]}{codeChars[11]}-" +
                         $"{codeChars[12]}{codeChars[13]}{codeChars[14]}{codeChars[15]}";

            var namePart = $"{codeChars[0]}{codeChars[1]}{codeChars[2]}{codeChars[3]}{codeChars[4]}{codeChars[5]}{codeChars[6]}{codeChars[7]}";
            var secretPart = $"{codeChars[8]}{codeChars[9]}{codeChars[10]}{codeChars[11]}{codeChars[12]}{codeChars[13]}{codeChars[14]}{codeChars[15]}";

            return new ScfLobbyInfo
            {
                FullCode = ("U/" + payload).ToUpperInvariant(),
                Identifier = new NetworkIdentifier
                {
                    Name = $"scaffolding-mc-{namePart[..4]}-{namePart[4..]}",
                    Secret = $"{secretPart[..4]}-{secretPart[4..]}"
                }
            };
        }
    }

    private static int MapToValue(char c)
    {
        var idx = Chars.IndexOf(char.ToUpperInvariant(c));
        if (idx >= 0) return idx;
        if (char.ToUpperInvariant(c) == 'I') return 1;
        if (char.ToUpperInvariant(c) == 'O') return 0;
        return 0;
    }

    /// <summary>
    /// 解析提示码
    /// </summary>
    public static bool TryParse(string input, out ScfLobbyInfo? info)
    {
        info = null;
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("U/", StringComparison.OrdinalIgnoreCase) || input.Length != 21)
            return false;

        var payload = input.AsSpan(2);
        var values = new int[DataLength];
        int idx = 0;

        for (int i = 0; i < payload.Length; i++)
        {
            var ch = payload[i];
            if (ch == '-') { if (i != 4 && i != 9 && i != 14) return false; continue; }
            if (idx >= DataLength) return false;
            var upper = char.ToUpperInvariant(ch);
            int val = Chars.IndexOf(upper);
            if (val < 0) { if (upper == 'I') val = 1; else if (upper == 'O') val = 0; else return false; }
            values[idx++] = val;
        }
        if (idx != DataLength) return false;

        // 验证整除性
        long checkValue = 0;
        for (int i = DataLength - 1; i >= 0; i--)
            checkValue = checkValue * BaseVal + values[i];
        if (checkValue % 7 != 0) return false;

        var nameChars = new char[8];
        var secretChars = new char[8];
        for (int i = 0; i < 8; i++) nameChars[i] = Chars[values[i]];
        for (int i = 0; i < 8; i++) secretChars[i] = Chars[values[i + 8]];

        var namePart = new string(nameChars);
        var secretPart = new string(secretChars);

        info = new ScfLobbyInfo
        {
            FullCode = input.ToUpperInvariant(),
            Identifier = new NetworkIdentifier
            {
                Name = $"scaffolding-mc-{namePart[..4]}-{namePart[4..]}",
                Secret = $"{secretPart[..4]}-{secretPart[4..]}"
            }
        };
        return true;
    }
}

// ============ Scaffolding 服务端 ============

public sealed class ScfServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly int _mcPort;
    private readonly string _playerName;
    private readonly string _machineId;
    private readonly string _vendor;
    private readonly ConcurrentDictionary<string, ScfPlayerProfile> _players = new();
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public event Action<IReadOnlyList<ScfPlayerProfile>>? PlayersUpdated;
    public int Port { get; }

    public ScfServer(int scfPort, int mcPort, string playerName, string machineId, string vendor)
    {
        Port = scfPort;
        _mcPort = mcPort;
        _playerName = playerName;
        _machineId = machineId;
        _vendor = vendor;
        _listener = new TcpListener(IPAddress.Loopback, scfPort);
        _players[machineId] = new ScfPlayerProfile { Name = playerName, MachineId = machineId, Vendor = vendor, Kind = PlayerRole.HOST };
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Start();
        _listenTask = ListenLoop(_cts.Token);
        _ = CleanupLoop(_cts.Token);
        // 触发一次初始玩家列表（包含房主自身）
        PlayersUpdated?.Invoke(_players.Values.ToList().AsReadOnly());
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct);
                _ = HandleClient(client, ct);
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            var stream = client.GetStream();
            var reader = PipeReader.Create(stream);
            var writer = PipeWriter.Create(stream);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var readResult = await reader.ReadAsync(ct);
                    var buffer = readResult.Buffer;
                    while (TryParseFrame(in buffer, out var typeInfo, out var body, out var endPos))
                    {
                        var response = typeInfo switch
                        {
                            "c:ping" => HandlePing(body),
                            "c:protocols" => HandleProtocols(),
                            "c:server_port" => HandleServerPort(),
                            "c:player_ping" => HandlePlayerPing(body),
                            "c:player_profiles_list" => HandlePlayerProfiles(),
                            _ => ((byte)255, Encoding.UTF8.GetBytes("Unknown request"))
                        };
                        await WriteResponseAsync(writer, response.Item1, response.Item2, ct);
                        buffer = buffer.Slice(endPos);
                    }
                    reader.AdvanceTo(buffer.Start, buffer.End);
                    if (readResult.IsCompleted) break;
                }
            }
            catch { }
        }
    }

    private static bool TryParseFrame(in ReadOnlySequence<byte> buffer, out string typeInfo, out byte[] body, out SequencePosition consumed)
    {
        typeInfo = ""; body = Array.Empty<byte>(); consumed = buffer.Start;
        if (buffer.Length < 1) return false;
        var reader = new SequenceReader<byte>(buffer);
        if (!reader.TryRead(out var typeLen)) return false;
        if (typeLen == 0 || typeLen > 128) return false;
        if (reader.Remaining < typeLen + 4) return false;
        Span<byte> typeSpan = stackalloc byte[typeLen];
        if (!reader.TryCopyTo(typeSpan)) return false;
        reader.Advance(typeLen);
        typeInfo = Encoding.UTF8.GetString(typeSpan);
        if (!reader.TryReadBigEndian(out int bodyLen32)) return false;
        var bodyLen = (uint)bodyLen32;
        if (bodyLen > 65536) return false;
        if (reader.Remaining < bodyLen) return false;
        body = reader.Sequence.Slice(reader.Position, bodyLen).ToArray();
        reader.Advance(bodyLen);
        consumed = reader.Position;
        return true;
    }

    private (byte, byte[]) HandlePing(byte[] body) => (0, body);

    private (byte, byte[]) HandleProtocols()
    {
        var protocols = "c:ping\0c:protocols\0c:server_port\0c:player_ping\0c:player_profiles_list";
        return (0, Encoding.ASCII.GetBytes(protocols));
    }

    private (byte, byte[]) HandleServerPort()
    {
        if (_mcPort == 0) return (32, Array.Empty<byte>());
        var bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)_mcPort);
        return (0, bytes);
    }

    private (byte, byte[]) HandlePlayerPing(byte[] body)
    {
        try
        {
            var profile = JsonSerializer.Deserialize<ScfPlayerProfile>(body,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            if (profile == null || string.IsNullOrEmpty(profile.MachineId)) return (32, Array.Empty<byte>());
            var guest = new ScfPlayerProfile { Name = profile.Name, MachineId = profile.MachineId, Vendor = profile.Vendor, Kind = PlayerRole.GUEST };
            var isNew = !_players.ContainsKey(guest.MachineId);
            _players[guest.MachineId] = guest;
            if (isNew) PlayersUpdated?.Invoke(_players.Values.ToList().AsReadOnly());
            return (0, Array.Empty<byte>());
        }
        catch { return (32, Array.Empty<byte>()); }
    }

    private (byte, byte[]) HandlePlayerProfiles()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(_players.Values.ToList(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        return (0, json);
    }

    private static async Task WriteResponseAsync(PipeWriter writer, byte status, byte[] body, CancellationToken ct)
    {
        var header = new byte[5];
        header[0] = status;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1), (uint)body.Length);
        await writer.WriteAsync(header, ct);
        if (body.Length > 0) await writer.WriteAsync(body, ct);
        await writer.FlushAsync(ct);
    }

    private async Task CleanupLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(10000, ct);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener.Stop();
        _cts?.Dispose();
    }
}

// ============ Scaffolding 客户端 ============

public sealed class ScfClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _playerName;
    private readonly string _machineId;
    private readonly string _vendor;
    private TcpClient? _tcp;
    private PipeReader? _reader;
    private PipeWriter? _writer;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CancellationTokenSource? _heartbeatCts;

    public event Action<IReadOnlyList<ScfPlayerProfile>>? PlayersUpdated;
    public event Action? ServerDisconnected;

    public bool IsConnected { get; private set; }
    public ushort? ServerPort { get; private set; }

    public ScfClient(string host, int port, string playerName, string machineId, string vendor)
    {
        _host = host; _port = port; _playerName = playerName; _machineId = machineId; _vendor = vendor;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(_host, _port, ct);
        var stream = _tcp.GetStream();
        _reader = PipeReader.Create(stream);
        _writer = PipeWriter.Create(stream);
        await SendPlayerPing(ct);
        IsConnected = true;
        await NegotiateProtocols(ct);
        ServerPort = await GetServerPort(ct);
        _heartbeatCts = new CancellationTokenSource();
        _ = HeartbeatLoop(_heartbeatCts.Token);
    }

    private async Task SendPlayerPing(CancellationToken ct)
    {
        var profile = new ScfPlayerProfile { Name = _playerName, MachineId = _machineId, Vendor = _vendor };
        var body = JsonSerializer.SerializeToUtf8Bytes(profile,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        await SendRequest("c:player_ping", body, ct);
    }

    private async Task NegotiateProtocols(CancellationToken ct)
    {
        var supported = "c:ping\0c:protocols\0c:server_port\0c:player_ping\0c:player_profiles_list";
        await SendRequest("c:protocols", Encoding.ASCII.GetBytes(supported), ct);
    }

    private async Task<ushort> GetServerPort(CancellationToken ct)
    {
        var (_, body) = await SendRequest("c:server_port", Array.Empty<byte>(), ct);
        if (body.Length >= 2) return BinaryPrimitives.ReadUInt16BigEndian(body);
        return 0;
    }

    private async Task HeartbeatLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, ct);
                await SendPlayerPing(ct);
                var profiles = await GetPlayerProfiles(ct);
                PlayersUpdated?.Invoke(profiles);
            }
            catch (OperationCanceledException) { break; }
            catch { ServerDisconnected?.Invoke(); break; }
        }
    }

    private async Task<IReadOnlyList<ScfPlayerProfile>> GetPlayerProfiles(CancellationToken ct)
    {
        var (_, body) = await SendRequest("c:player_profiles_list", Array.Empty<byte>(), ct);
        return JsonSerializer.Deserialize<List<ScfPlayerProfile>>(body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })
            ?? new List<ScfPlayerProfile>();
    }

    private async Task<(byte, byte[])> SendRequest(string type, byte[] body, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await ScfProtocolWriter.WriteRequestAsync(_writer!, type, body, ct);
            return await ScfProtocolReader.ReadResponseAsync(_reader!, ct);
        }
        finally { _lock.Release(); }
    }

    public void Dispose()
    {
        _heartbeatCts?.Cancel();
        _tcp?.Dispose();
        _heartbeatCts?.Dispose();
        IsConnected = false;
    }
}

// ============ 主服务类 ============

public class ETModeService : IDisposable
{
    // 平台检测
    private static bool IsWindows => OperatingSystem.IsWindows();
    private static bool IsLinux => OperatingSystem.IsLinux();
    private static bool IsMacOS => OperatingSystem.IsMacOS();
    private static bool IsArm64 => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

    // EasyTier 版本和路径
    private const string ETVersion = "2.6.4";
    private string ETDirectory => Path.Combine(Path.GetTempPath(), "MCZLFAPP", "ET", ETVersion);
    private string ETCorePath => Path.Combine(ETDirectory, IsWindows ? "easytier-core.exe" : "easytier-core");
    private string ETCliPath => Path.Combine(ETDirectory, IsWindows ? "easytier-cli.exe" : "easytier-cli");

    // 下载 URL
    private string DownloadUrl
    {
        get
        {
            string os, arch;
            if (IsWindows) { os = "windows"; arch = IsArm64 ? "arm64" : "x86_64"; }
            else if (IsLinux) { os = "linux"; arch = IsArm64 ? "aarch64" : "x86_64"; }
            else if (IsMacOS) { os = "macos"; arch = IsArm64 ? "aarch64" : "x86_64"; }
            else throw new PlatformNotSupportedException();
            return $"https://v6.gh-proxy.org/https://github.com/EasyTier/EasyTier/releases/download/v{ETVersion}/easytier-{os}-{arch}-v{ETVersion}.zip";
        }
    }

    // 公共节点获取
    private const string PublicNodeApiUrl = "https://uptime.easytier.cn/api/nodes?page=1&per_page=50&is_active=true";
    private const string RuixuanApiUrl = "https://ruixuan.online/uptime/api/status-page/easytier";
    private static readonly string[] FallbackNodes =
    [
        "tcp://et1.fuis.top:11010",
        "tcp://225284.xyz:11010"
    ];

    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly Regex NodeUrlRegex = new(@"((tcp|udp|wss?)://[^\s（(]+:\d+)", RegexOptions.Compiled);

    private static async Task<List<string>> FetchPublicNodesAsync()
    {
        var result = new List<string>();

        // 1. 尝试 uptime.easytier.cn API
        try
        {
            var response = await _httpClient.GetStringAsync(PublicNodeApiUrl);
            var dto = JsonSerializer.Deserialize<PublicNodeDto>(response);
            if (dto is { IsSuccess: true, Data.Items: not null })
            {
                result.AddRange(dto.Data.Items
                    .Where(it => it is { IsActive: true, IsAllowRelay: true })
                    .Where(it => !it.Host.Contains('*'))
                    .Select(it => it.Host));
            }
        }
        catch { }

        // 2. 尝试 ruixuan.online 监控 API（仅国内节点）
        try
        {
            var response = await _httpClient.GetStringAsync(RuixuanApiUrl);
            var ruixuanDto = JsonSerializer.Deserialize<RuixuanStatusDto>(response);
            if (ruixuanDto?.PublicGroupList != null)
            {
                foreach (var group in ruixuanDto.PublicGroupList)
                {
                    // 只取 "社区公共节点" 组，跳过海外节点和 Web 控制台
                    if (group.Name == null) continue;
                    if (group.Name.Contains("海外")) continue;
                    if (group.Name.Contains("web控制台") || group.Name.Contains("web控制台")) continue;
                    if (!group.Name.Contains("社区公共节点")) continue;

                    foreach (var monitor in group.MonitorList ?? [])
                    {
                        // 跳过包含 * 的打码节点和海外节点
                        var name = monitor.Name ?? "";
                        if (name.Contains('*')) continue;
                        if (name.Contains("海外")) continue;

                        var match = NodeUrlRegex.Match(name);
                        if (match.Success)
                        {
                            var url = match.Groups[1].Value;
                            if (!result.Contains(url))
                                result.Add(url);
                        }
                    }
                }
            }
        }
        catch { }

        // 3. 补充 fallback 节点（仅国内）
        foreach (var node in FallbackNodes)
        {
            if (!result.Contains(node))
                result.Add(node);
        }

        // 过滤掉所有含 * 的节点（以防万一）
        result = result.Where(n => !n.Contains('*')).ToList();

        return result;
    }

    // ============ 单例 ============
    private static ETModeService? _instance;
    public static ETModeService Instance => _instance ??= new ETModeService();

    // 状态
    private ETCoreState _state = ETCoreState.Stopped;
    private Process? _etProcess;
    private int _rpcPort;
    private ScfServer? _scfServer;
    private ScfClient? _scfClient;
    private ScfLobbyInfo? _lobbyInfo;
    private int _mcPort;
    private string _machineId = "";
    private string _playerName = "";
    private int _lastScfPlayerCount = -1;
    private IReadOnlyList<ScfPlayerProfile> _currentScfPlayers = Array.Empty<ScfPlayerProfile>();

    public ETCoreState State => _state;
    public ScfLobbyInfo? LobbyInfo => _lobbyInfo;

    // 事件
    public event EventHandler<string>? LogMessage;
    public event EventHandler<double>? ProgressChanged;
    public event EventHandler? CoreStarted;
    public event EventHandler? CoreStopped;
    public event EventHandler<string>? PromptCodeGenerated;
    public event EventHandler<int>? ServerPortDetected;
    public event EventHandler<IReadOnlyList<ETPlayerInfo>>? PlayerListUpdated;
    public event EventHandler<IReadOnlyList<ScfPlayerProfile>>? ScfPlayersUpdated;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// 当前 SCF 玩家列表缓存（用于新面板初始化时获取当前数据）
    /// </summary>
    public IReadOnlyList<ScfPlayerProfile> CurrentScfPlayers => _currentScfPlayers;

    /// <summary>
    /// 最近一次 PollPlayerList 获取到的第一个 peer（用于显示 Ping/连接类型）
    /// </summary>
    public ETPlayerInfo? LastEtPeer { get; private set; }

    private void Log(string msg)
    {
        LogMessage?.Invoke(this, msg);
        try
        {
            var logDir = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(Path.Combine(logDir, "APPLog.ini"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ET] {msg}{Environment.NewLine}");
        }
        catch { }
    }

    /// <summary>
    /// 检查并下载 EasyTier 核心
    /// </summary>
    public async Task<bool> EnsureEasyTierAsync()
    {
        if (File.Exists(ETCorePath) && File.Exists(ETCliPath))
        {
            Log("EasyTier 核心已存在");
            return true;
        }

        Log("开始下载 EasyTier 核心...");
        ProgressChanged?.Invoke(this, 0);
        Directory.CreateDirectory(ETDirectory);

        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            long downloaded = 0;
            var zipPath = Path.Combine(ETDirectory, "easytier.zip");

            using (var httpStream = await response.Content.ReadAsStreamAsync())
            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = await httpStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloaded += bytesRead;
                    if (totalBytes > 0) ProgressChanged?.Invoke(this, (double)downloaded / totalBytes * 100);
                }
            }

            Log("下载完成，正在解压...");
            ProgressChanged?.Invoke(this, 0);

            ZipFile.ExtractToDirectory(zipPath, ETDirectory, true);
            File.Delete(zipPath);

            // EasyTier zip 包内有嵌套目录（如 easytier-windows-x86_64-v2.6.4/），需要将文件移到根目录
            if (!File.Exists(ETCorePath))
            {
                var nestedDirs = Directory.GetDirectories(ETDirectory);
                foreach (var nestedDir in nestedDirs)
                {
                    var nestedCore = Path.Combine(nestedDir, IsWindows ? "easytier-core.exe" : "easytier-core");
                    if (File.Exists(nestedCore))
                    {
                        // 移动所有文件到 ETDirectory
                        foreach (var file in Directory.GetFiles(nestedDir))
                        {
                            var dest = Path.Combine(ETDirectory, Path.GetFileName(file));
                            File.Move(file, dest, true);
                        }
                        foreach (var dir in Directory.GetDirectories(nestedDir))
                        {
                            var dest = Path.Combine(ETDirectory, Path.GetFileName(dir));
                            if (Directory.Exists(dest)) Directory.Delete(dest, true);
                            Directory.Move(dir, dest);
                        }
                        Directory.Delete(nestedDir, true);
                        Log($"已从嵌套目录 {Path.GetFileName(nestedDir)} 中提取文件");
                        break;
                    }
                }
            }

            if (!IsWindows)
            {
                try
                {
                    var chmod = new Process { StartInfo = new ProcessStartInfo { FileName = "chmod", Arguments = $"+x \"{ETCorePath}\" \"{ETCliPath}\"", UseShellExecute = false, CreateNoWindow = true } };
                    chmod.Start();
                    await chmod.WaitForExitAsync();
                }
                catch { }
            }

            ProgressChanged?.Invoke(this, 100);
            Log("EasyTier 核心安装完成");
            return true;
        }
        catch (Exception ex)
        {
            Log($"下载 EasyTier 失败: {ex.Message}");
            ProgressChanged?.Invoke(this, 0);
            return false;
        }
    }

    private string GenerateMachineId()
    {
        var raw = $"{Environment.MachineName}-{Environment.UserName}-{Environment.OSVersion}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()[..16];
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// 开启联机房间（房主模式）
    /// </summary>
    public async Task<bool> StartHostAsync(int mcPort, string playerName)
    {
        if (_state != ETCoreState.Stopped) { Log("ET核心已在运行中"); return false; }

        _machineId = GenerateMachineId();
        _playerName = playerName;
        _mcPort = mcPort;
        _lastScfPlayerCount = -1;
        _currentScfPlayers = Array.Empty<ScfPlayerProfile>();

        Log("正在启动 ET 联机房间...");

        if (!await EnsureEasyTierAsync()) return false;

        // 生成提示码
        _lobbyInfo = ScfLobbyCodeGenerator.Generate();
        Log($"提示码: {_lobbyInfo.FullCode}");
        PromptCodeGenerated?.Invoke(this, _lobbyInfo.FullCode);

        // 分配端口
        _rpcPort = GetAvailablePort();
        var scfPort = GetAvailablePort();
        Log($"Scaffolding 端口: {scfPort}, MC 端口: {mcPort}, RPC 端口: {_rpcPort}");

        // 构造启动参数
        Log("正在获取公共节点列表...");
        var publicNodes = await FetchPublicNodesAsync();
        Log($"已获取 {publicNodes.Count} 个公共节点");
        var args = BuildHostArgs(scfPort, publicNodes);
        Log($"EasyTier 启动参数: {args}");

        if (!StartETProcess(args)) { Log("EasyTier 启动失败"); return false; }

        await Task.Delay(2000);

        // 启动 Scaffolding 服务端
        _scfServer = new ScfServer(scfPort, mcPort, playerName, _machineId, $"MCT {Views.MainWindow.version}, Scaffolding");
        _scfServer.PlayersUpdated += OnScfPlayersUpdated;
        _scfServer.Start();
        Log("Scaffolding 服务端已启动");

        _state = ETCoreState.Running;
        CoreStarted?.Invoke(this, EventArgs.Empty);
        StatusChanged?.Invoke(this, "等待玩家加入...");
        Log("联机房间已开启，等待玩家加入");
        Log("请让朋友使用支持 SCF 协议的启动器或联机工具加入~");

        _ = PollPlayerListAsync();
        return true;
    }

    /// <summary>
    /// 加入联机房间（房客模式，端口由SCF协议自动协商）
    /// </summary>
    public async Task<bool> StartJoinAsync(string promptCode, string playerName)
    {
        if (_state != ETCoreState.Stopped) { Log("ET核心已在运行中"); return false; }

        _machineId = GenerateMachineId();
        _playerName = playerName;
        _mcPort = 0;
        _lastScfPlayerCount = -1;
        _currentScfPlayers = Array.Empty<ScfPlayerProfile>();

        // 解析提示码
        if (!ScfLobbyCodeGenerator.TryParse(promptCode, out var lobby) || lobby == null)
        {
            Log("无效的提示码格式");
            return false;
        }
        _lobbyInfo = lobby;
        Log($"解析提示码: {lobby.FullCode}");
        Log($"网络名称: {lobby.Identifier.Name}, 网络密钥: {lobby.Identifier.Secret}");

        if (!await EnsureEasyTierAsync()) return false;

        _rpcPort = GetAvailablePort();

        Log("正在获取公共节点列表...");
        var publicNodes = await FetchPublicNodesAsync();
        Log($"已获取 {publicNodes.Count} 个公共节点");
        var args = BuildJoinArgs(publicNodes);
        Log($"EasyTier 启动参数: {args}");

        if (!StartETProcess(args)) { Log("EasyTier 启动失败"); return false; }

        StatusChanged?.Invoke(this, "正在查找房主...");
        Log("正在等待 EasyTier 网络就绪...");

        // 等待网络就绪并获取玩家列表
        int retryCount = 0;
        string? hostIp = null;
        int scfPort = 0;

        while (retryCount < 30)
        {
            await Task.Delay(1000);
            var players = GetCliPeerList();
            if (players != null)
            {
                var host = players.FirstOrDefault(p => p.Hostname.StartsWith("scaffolding-mc-server-"));
                if (host != null)
                {
                    hostIp = host.Ipv4;
                    var portStr = host.Hostname["scaffolding-mc-server-".Length..];
                    if (int.TryParse(portStr, out scfPort))
                    {
                        Log($"找到房主: {host.Hostname} ({hostIp}:{scfPort})");
                        break;
                    }
                }
            }
            retryCount++;
            Log($"等待房主发现... ({retryCount}/30)");
        }

        if (hostIp == null || scfPort == 0)
        {
            Log("未找到房主，联机失败");
            await StopETAsync();
            return false;
        }

        // 建立端口转发
        var localScfPort = GetAvailablePort();
        if (!AddPortForward(localScfPort, hostIp, scfPort))
        {
            Log("端口转发失败");
            await StopETAsync();
            return false;
        }
        Log($"端口转发: 127.0.0.1:{localScfPort} -> {hostIp}:{scfPort}");

        // 启动 Scaffolding 客户端
        _scfClient = new ScfClient("127.0.0.1", localScfPort, playerName, _machineId, $"MCT {Views.MainWindow.version}, Scaffolding");
        _scfClient.PlayersUpdated += OnScfPlayersUpdated;
        _scfClient.ServerDisconnected += () =>
        {
            Log("与房主的连接断开");
            ErrorOccurred?.Invoke(this, "与房主的连接断开");
        };

        try
        {
            await _scfClient.ConnectAsync();
            Log("已连接到房主的 Scaffolding 服务");
        }
        catch (Exception ex)
        {
            Log($"连接 Scaffolding 服务失败: {ex.Message}");
            await StopETAsync();
            return false;
        }

        // 获取 MC 服务器端口并转发
        var serverPort = _scfClient.ServerPort;
        if (serverPort.HasValue && serverPort.Value > 0)
        {
            // 确定本地 MC 转发端口：优先使用用户配置，否则随机分配
            int localMcPort;
            var customPort = ConfigService.Read<int>("ETCustomPort", 0);
            if (customPort > 0 && customPort <= 65535)
            {
                localMcPort = customPort;
            }
            else
            {
                localMcPort = GetAvailablePort();
            }

            if (AddPortForward(localMcPort, hostIp, serverPort.Value))
            {
                _mcPort = localMcPort;
                Log($"MC 服务器转发: 127.0.0.1:{localMcPort} -> {hostIp}:{serverPort}");
                ServerPortDetected?.Invoke(this, localMcPort);
            }
        }

        _state = ETCoreState.Running;
        CoreStarted?.Invoke(this, EventArgs.Empty);
        StatusChanged?.Invoke(this, "已连接");

        _ = PollPlayerListAsync();
        return true;
    }

    /// <summary>
    /// 停止 ET 核心
    /// </summary>
    public async Task<bool> StopETAsync()
    {
        if (_state == ETCoreState.Stopped) return true;

        Log("正在停止 ET 核心...");
        _state = ETCoreState.Stopped;

        _scfServer?.Dispose();
        _scfServer = null;
        _scfClient?.Dispose();
        _scfClient = null;

        if (_etProcess != null)
        {
            try
            {
                if (!_etProcess.HasExited)
                {
                    _etProcess.Kill(true);
                    await _etProcess.WaitForExitAsync();
                }
            }
            catch { }
            _etProcess.Dispose();
            _etProcess = null;
        }

        // 确保所有 easytier-core 进程都被清理
        try
        {
            foreach (var p in Process.GetProcessesByName("easytier-core"))
            {
                try { p.Kill(); p.Dispose(); } catch { }
            }
        }
        catch { }

        CoreStopped?.Invoke(this, EventArgs.Empty);
        Log("ET 核心已停止");
        return true;
    }

    // ============ 内部方法 ============

    private string BuildHostArgs(int scfPort, List<string> nodes)
    {
        var identifier = _lobbyInfo!.Identifier;
        var sb = new StringBuilder();
        sb.Append($"--no-tun --multi-thread");
        sb.Append($" --network-name {identifier.Name}");
        sb.Append($" --network-secret {identifier.Secret}");
        sb.Append($" --hostname scaffolding-mc-server-{scfPort}");
        sb.Append($" --ipv4 10.114.51.41");
        sb.Append($" --rpc-portal {_rpcPort}");
        sb.Append($" --machine-id {_machineId}");
        sb.Append($" --private-mode true");
        sb.Append($" --tcp-whitelist {scfPort}");
        sb.Append($" --udp-whitelist {scfPort}");
        sb.Append($" --tcp-whitelist {_mcPort}");
        sb.Append($" --udp-whitelist {_mcPort}");
        sb.Append($" --listeners tcp://0.0.0.0:0");
        sb.Append($" --listeners udp://0.0.0.0:0");
        sb.Append($" --enable-kcp-proxy");
        sb.Append($" --enable-quic-proxy");
        sb.Append($" --use-smoltcp");
        sb.Append($" --compression zstd");
        sb.Append($" --default-protocol tcp");
        sb.Append($" --encryption-algorithm aes-gcm");
        foreach (var node in nodes) sb.Append($" --peers {node}");
        return sb.ToString();
    }

    private string BuildJoinArgs(List<string> nodes)
    {
        var identifier = _lobbyInfo!.Identifier;
        var sb = new StringBuilder();
        sb.Append($"--no-tun --multi-thread -d");
        sb.Append($" --network-name {identifier.Name}");
        sb.Append($" --network-secret {identifier.Secret}");
        sb.Append($" --hostname {_machineId}|{_playerName}");
        sb.Append($" --rpc-portal {_rpcPort}");
        sb.Append($" --machine-id {_machineId}");
        sb.Append($" --private-mode true");
        sb.Append($" --tcp-whitelist 0");
        sb.Append($" --udp-whitelist 0");
        sb.Append($" --listeners tcp://0.0.0.0:0");
        sb.Append($" --listeners udp://0.0.0.0:0");
        sb.Append($" --enable-kcp-proxy");
        sb.Append($" --enable-quic-proxy");
        sb.Append($" --use-smoltcp");
        sb.Append($" --compression zstd");
        sb.Append($" --default-protocol tcp");
        sb.Append($" --encryption-algorithm aes-gcm");
        foreach (var node in nodes) sb.Append($" --peers {node}");
        return sb.ToString();
    }

    private bool StartETProcess(string arguments)
    {
        try
        {
            _etProcess = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = ETCorePath,
                    WorkingDirectory = ETDirectory,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
            _etProcess.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log($"[ET] {e.Data}"); };
            _etProcess.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log($"[ET] {e.Data}"); };
            _etProcess.Exited += (_, _) => { if (_state != ETCoreState.Stopped) { _state = ETCoreState.Stopped; CoreStopped?.Invoke(this, EventArgs.Empty); } };
            _etProcess.Start();
            _etProcess.BeginOutputReadLine();
            _etProcess.BeginErrorReadLine();
            Log("EasyTier 进程已启动");
            return true;
        }
        catch (Exception ex)
        {
            Log($"启动 EasyTier 进程失败: {ex.Message}");
            return false;
        }
    }

    private List<ETCliPeerInfo>? GetCliPeerList()
    {
        try
        {
            var cli = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ETCliPath,
                    WorkingDirectory = ETDirectory,
                    Arguments = $"--rpc-portal 127.0.0.1:{_rpcPort} -o json peer",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
            cli.Start();
            var output = cli.StandardOutput.ReadToEnd();
            cli.WaitForExit(3000);
            if (string.IsNullOrWhiteSpace(output)) return null;
            return JsonSerializer.Deserialize<List<ETCliPeerInfo>>(output,
                new JsonSerializerOptions { PropertyNamingPolicy = null });
        }
        catch { return null; }
    }

    private bool AddPortForward(int localPort, string remoteIp, int remotePort)
    {
        try
        {
            // TCP 转发
            var tcpFwd = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ETCliPath,
                    WorkingDirectory = ETDirectory,
                    Arguments = $"--rpc-portal 127.0.0.1:{_rpcPort} port-forward add tcp 127.0.0.1:{localPort} {remoteIp}:{remotePort}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            tcpFwd.Start();
            tcpFwd.WaitForExit(3000);

            // UDP 转发
            var udpFwd = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ETCliPath,
                    WorkingDirectory = ETDirectory,
                    Arguments = $"--rpc-portal 127.0.0.1:{_rpcPort} port-forward add udp 127.0.0.1:{localPort} {remoteIp}:{remotePort}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            udpFwd.Start();
            udpFwd.WaitForExit(3000);
            return true;
        }
        catch { return false; }
    }

    private async Task PollPlayerListAsync()
    {
        while (_state != ETCoreState.Stopped)
        {
            try
            {
                await Task.Delay(3000);
                if (_state == ETCoreState.Stopped) break;

                var cliPeers = GetCliPeerList();
                if (cliPeers == null) continue;

                // 只保留 SCF 玩家：房主（hostname 以 scaffolding-mc-server- 开头）
                // 或房客（hostname 包含 | 分隔符，格式为 machine_id|username）
                // 排除中继节点和自身节点
                var isHostMode = _lobbyInfo != null && _scfServer != null;
                var players = new List<ETPlayerInfo>();
                var selfHostname = isHostMode ? $"scaffolding-mc-server-{_mcPort}" : $"{_machineId}|{_playerName}";

                // 先添加本地玩家（房主或房客自身）
                var localPeer = cliPeers.FirstOrDefault(p =>
                    p.Hostname.Contains(_machineId) || p.Hostname == _machineId || (_lobbyInfo != null && p.Hostname.Contains(_lobbyInfo.Identifier.Name)));
                players.Add(new ETPlayerInfo
                {
                    IsHost = isHostMode,
                    Hostname = selfHostname,
                    Username = _playerName,
                    VirtualIp = localPeer?.Ipv4 ?? "",
                    ConnectionType = ETConnectionType.Local,
                    Ping = 0,
                    Loss = 0,
                    NatType = localPeer?.NatType ?? "",
                    Vendor = $"MCT {Views.MainWindow.version}, Scaffolding"
                });

                // 再添加远程玩家（排除自身）
                players.AddRange(cliPeers
                    .Where(p => p.Hostname.StartsWith("scaffolding-mc-server-") || p.Hostname.Contains('|'))
                    .Where(p => p.Hostname != selfHostname)
                    .Where(p => !p.Hostname.Contains(_machineId))
                    .Select(p =>
                    {
                        var isHost = p.Hostname.StartsWith("scaffolding-mc-server-");
                        var parts = p.Hostname.Split('|');
                        return new ETPlayerInfo
                        {
                            IsHost = isHost,
                            Hostname = p.Hostname,
                            Username = !isHost && parts.Length >= 2 ? parts[1] : (isHost ? "房主" : null),
                            VirtualIp = p.Ipv4,
                            ConnectionType = p.Cost.Contains("p2p", StringComparison.OrdinalIgnoreCase) ? ETConnectionType.P2P
                                : p.Cost.Contains("relay", StringComparison.OrdinalIgnoreCase) ? ETConnectionType.Relay
                                : p.Cost.Contains("local", StringComparison.OrdinalIgnoreCase) ? ETConnectionType.Local
                                : ETConnectionType.Unknown,
                            Ping = Math.Round(double.TryParse(p.LatMs, out var lat) ? lat : 0),
                            Loss = Math.Round(double.TryParse(p.LossRate.Replace("%", ""), out var loss) ? loss : 0),
                            NatType = p.NatType,
                            Vendor = p.Version
                        };
                    }));

                if (_state == ETCoreState.Running)
                {
                    _state = ETCoreState.Ready;
                    StatusChanged?.Invoke(this, "已就绪");
                }

                LastEtPeer = players.FirstOrDefault();
                PlayerListUpdated?.Invoke(this, players.AsReadOnly());
            }
            catch { }
        }
    }

    private void OnScfPlayersUpdated(IReadOnlyList<ScfPlayerProfile> profiles)
    {
        // 仅在人数变化时记录日志，避免刷屏
        var count = profiles.Count;
        if (count != _lastScfPlayerCount)
        {
            _lastScfPlayerCount = count;
            Log($"Scaffolding 玩家列表更新: {count} 人");
        }
        // 缓存最新玩家列表
        _currentScfPlayers = profiles;
        // 向 ETRoomList 等订阅者推送真实 SCF 玩家列表
        ScfPlayersUpdated?.Invoke(this, profiles);
    }

    public void Dispose()
    {
        _ = StopETAsync();
    }
}
