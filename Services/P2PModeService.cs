using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MinecraftConnectTool;

namespace MinecraftConnectTool.Services;

/// <summary>
/// P2P模式核心服务 - 紧密衔接原WinForm逻辑
/// </summary>
public class P2PModeService : IDisposable
{
    // Token配置
    public string tokenNormal = "17073157824633806511";
    public string tokenTest = "7196174974940052261";
    
    // role：0=未开启, 1=房主, 2=加入方
    public string role = "0";
    
    // 当前提示码/加入地址
    public string tsm = "";
    public string user = "";
    public string port = "";
    
    // 配置项 - 改为属性实时读取
    public bool useoldway = false;
    public bool admin = false;
    
    /// <summary>
    /// 是否启用ServerPost多播 - 实时从配置读取
    /// </summary>
    public bool EnableServerPost => ConfigService.Read("ServerPostEnable", true);
    
    /// <summary>
    /// 是否启用TCP模式 - 实时从配置读取
    /// </summary>
    public bool TCP => ConfigService.Read("TCP", true);
    
    // 核心文件路径
    private string CoreDirectory => Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
    private string CoreFilePath => Path.Combine(CoreDirectory, "main.exe");
    
    // MD5校验值
    private string Md5Normal => "08160296509deac13e7d12c8754de9ef";
    private string Md5Admin => "29d76fc2626c66925621d475f3a6827a";
    private string Md532Bit => "640ffdaa2a7b249d9c301102419a69cb";
    
    // 当前进程
    private Process? _currentProcess;
    
    // 事件
    public event EventHandler<string>? LogMessage;
    public event EventHandler<double>? ProgressChanged;
    public event EventHandler<string>? TsmGenerated;
    public event EventHandler? CoreStarted;
    public event EventHandler? CoreStopped;
    
    public P2PModeService()
    {
        useoldway = false; // 默认新版方式
    }
    
    /// <summary>
    /// 记录日志 - 包含替换词和状态指示器逻辑
    /// </summary>
    private void log(string message)
    {
        // 1. 替换词处理
        var replacements = new Dictionary<string, string>
        {
            { "{tokenNormal}", "MinecraftConnectTool" },
            { "16947733", "690625244" },
            { "openp2p.cn@gmail.com", "admin@mczlf.xyz" },
            { "openp2p start", "Powered by OpenP2P" }
        };
        foreach (var pair in replacements)
        {
            message = Regex.Replace(message, @"\b" + Regex.Escape(pair.Key) + @"\b", pair.Value);
        }

        // 2. 写入日志文件
        string logFilePath = Path.Combine(
            Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
            "MCZLFAPP", "Temp", "APPLog.ini");
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
        File.AppendAllText(logFilePath, logMessage);

        // 3. 状态指示器处理
        ProcessLogIndicators(message);

        // 4. 触发日志事件
        LogMessage?.Invoke(this, message);
    }

    /// <summary>
    /// 处理日志中的状态指示器 - 严格按照原WinForm逻辑
    /// </summary>
    private void ProcessLogIndicators(string message)
    {
        // 定义字符与方法的映射关系（原LogToRichTextBox中的字典）
        var methodMap = new Dictionary<string, Action>
        {
            { "3.21.12", () => log("LogListener已成功被加载") },
            { "NAT type", () => HandleNatType(message) },
            { "login ok", () => StatusChanged?.Invoke(this, ("Warn", "正在处理中...|与服务器交换信息")) },
            { "P2PNetwork init start", () => log("正在尝试连接P2PNetwork") },
            { "NAT detect error", () => log("NAT类型探测失败 i/o timeout") },
            { "LISTEN ON PORT", () => { 
                log("Success:成功在本地创建监听端口"); 
                StatusChanged?.Invoke(this, ("Success", "已连接")); 
            }},
            { "sdwan init ok", () => {
                string badgeText = role == "1" ? "正在等待被玩家连接,快去邀请好友加入房间吧" : "正在尝试连接...";
                string badgeState = role == "1" ? "Waiting" : "Warn";
                StatusChanged?.Invoke(this, (badgeState, badgeText));
            }},
            { "connection ok", () => {
                string badgeState = role == "1" ? "Success" : "Warn";
                StatusChanged?.Invoke(this, (badgeState, "已连接"));
            }},
            { "handShakeC2C ok", () => {
                string badgeState = role == "1" ? "Success" : "Warn";
                StatusChanged?.Invoke(this, (badgeState, "已连接"));
            }},
            { "i/o timeout", () => log("i/o超时,可能是端口无法连接,也可能是运营商动手脚了,建议检查一下端口是否有误并继续等待") },
            { "no such host", () => ErrorOccurred?.Invoke(this, "程序未能够连接到HOST,可能是防火墙拦截,或是根本没有授予网络访问权限") },
            { "it will auto reconnect when peer node online", () => ErrorOccurred?.Invoke(this, "房间不在线,请检查是否有输入错误,或好友是否正确的启动了房间") },
            { "peer offline", () => {
                StatusChanged?.Invoke(this, ("Error", "对方不在线"));
                ErrorOccurred?.Invoke(this, "对方不在线,请检查是否有输入错误,或好友是否正确的启动了房间");
            }},
            { "Usage:", () => {
                _ = stopp2p();
                StatusChanged?.Invoke(this, ("Error", "触发Usage_Bug"));
                ErrorOccurred?.Invoke(this, "这是一个Bug,你可以反馈一下发生了什么吗?\n请不要直接对着这个窗口拍照，请上传完整日志");
            }},
        };

        // 检查消息中是否包含特定字符
        foreach (var pair in methodMap)
        {
            if (message.Contains(pair.Key))
            {
                pair.Value.Invoke();
            }
        }
    }

    /// <summary>
    /// 处理NAT类型 - 完整实现原WinForm逻辑
    /// </summary>
    private void HandleNatType(string message)
    {
        string pattern = @"NAT type\s*:\s*(\w+)";
        var match = Regex.Match(message, pattern);

        if (match.Success)
        {
            string natType = match.Groups[1].Value;

            if (natType == "2" || natType.ToLower() == "symmetric")
            {
                // 对称型NAT警告
                log("当前NAT类型为对称形 Symmetric NAT,可能连接时间要非常久...或者可能连不上");
                NatTypeDetected?.Invoke(this, $"Symmetric NAT (Type {natType}) - 可能连接困难");
            }
            else
            {
                log($"NatType:{natType},Support");
                NatTypeDetected?.Invoke(this, $"NAT Type {natType} - 支持");
            }
        }
        else
        {
            log("NatType:?");
            NatTypeDetected?.Invoke(this, "未知NAT类型");
        }
    }

    // 事件定义
    public event EventHandler<string>? NatTypeDetected;
    public event EventHandler<(string Status, string Detail)>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    
    /// <summary>
    /// 房主模式 - 开启联机房间（紧密衔接原Opener_Click逻辑）
    /// </summary>
    public async Task<bool> StartHostAsync()
    {
        role = "1"; //房主
        
        // 检查是否已有核心在运行 - 参考原WinForm逻辑，仅提示不阻止
        if (Process.GetProcessesByName("main").Length > 0)
        {
            AlreadyCore();
        }
        
        log("检查P2PMode核心中..");
        
        string localName = Environment.MachineName;
        Random random = new Random();
        int randomPort = random.Next(1, 65536);
        
        // 确保目录存在
        Directory.CreateDirectory(CoreDirectory);
        Directory.SetCurrentDirectory(CoreDirectory);
        
        // 清理运行垃圾
        if (File.Exists("config.json")) File.Delete("config.json");
        if (File.Exists("config.json0")) File.Delete("config.json0");
        
        // 增强提醒 - 生成提示码
        int codeupate = ConfigService.Read("codeupdate", 1);
        if (codeupate == 1)
        {
            tsm = $"{localName}{randomPort}";
        }
        else if (codeupate == 2)
        {
            tsm = $"{localName}{DateTime.Now:yyyyMMddHH}HH";
        }
        else if (codeupate == 3)
        {
            tsm = $"{localName}{DateTime.Now:yyyyMMdd}DD";
        }
        else if (codeupate == 4) //永久
        {
            if (ConfigService.Read("usecustomnode", false))
            {
                tsm = ConfigService.Read("customnode", "");
                // 敏感词检测
                if (!await CheckProfanityAsync(tsm))
                {
                    await stopp2p();
                    return false;
                }
            }
            else
            {
                tsm = $"{localName}{randomPort}";
            }
        }
        
        // 检查长度要求
        if (tsm.Length <= 8)
        {
            int threeDigitRandom = random.Next(100, 1000);
            tsm += threeDigitRandom.ToString();
            log("提示码不满足要求,已自动加入三位随机数字");
        }
        
        // OLAN兼容模式
        bool enableOLAN = ConfigService.Read("EnableOLAN", false);
        if (enableOLAN)
        {
            tsm = "M" + tsm + "C";
        }
        
        // 注意：TsmGenerated 事件在下载完成后触发，避免 InfoBar 遮挡进度条
        log($"您的提示码为 {tsm}");
        
        // 确定下载URL和MD5
        string url;
        string fileMd5;
        if (Environment.Is64BitOperatingSystem)
        {
            if (admin)
            {
                url = "https://api.mct.mczlf.loft.games/main32413.exe";
                fileMd5 = Md5Admin;
            }
            else
            {
                url = "https://api.mct.mczlf.loft.games/mainnew.exe";
                fileMd5 = Md5Normal;
            }
        }
        else
        {
            url = "https://api.mct.mczlf.loft.games/mainnew32.exe";
            fileMd5 = Md532Bit;
        }
        
        // 检查并下载核心
        bool needsDownload = false;
        if (File.Exists(CoreFilePath))
        {
            string? md5Hash = GetFileMD5Hash(CoreFilePath);
            if (md5Hash == fileMd5)
            {
                log("64位核心已存在且安全校验通过");
            }
            else if (md5Hash == Md532Bit)
            {
                log("32位核心已存在且安全校验通过");
            }
            else if (md5Hash == null)
            {
                log("出现错误，进程终止");
                role = "0";
                return false;
            }
            else
            {
                log("核心不存在或安全校验不通过,重新Download中");
                needsDownload = true;
            }
        }
        else
        {
            log("核心不存在或安全校验不通过,重新Download中");
            needsDownload = true;
        }
        
        // 下载核心
        if (needsDownload)
        {
            ProgressChanged?.Invoke(this, 0); // 开始下载，进度归零
            log("Downloader启动");

            bool downloadSuccess = await DownloadCoreAsync(url, CoreFilePath);

            ProgressChanged?.Invoke(this, 0); // 下载结束，进度归零

            if (!downloadSuccess)
            {
                role = "0";
                return false;
            }
        }
        
        log("Task:下载核心中..");
        
        // 下载完成后显示提示信息（避免 InfoBar 遮挡下载进度条）
        TsmGenerated?.Invoke(this, tsm);
        
        // 复制邀请信息到剪贴板
        try
        {
            string inviteText = $"邀请你加入我的Minecraft联机房间！\n提示码为 {tsm}\n复制时请勿带上前面的中文哦";
            _ = ClipboardHelper.SetTextAsync(inviteText);
            log("邀请信息已复制到剪贴板");
        }
        catch (Exception ex)
        {
            log($"复制邀请信息失败: {ex.Message}");
        }
        
        // 构造启动参数
        string arguments;
        if (TCP)
        {
            arguments = $"-node {tsm} -token {tokenNormal}";
        }
        else
        {
            arguments = $"-node {tsm} -protocol udp -token {tokenTest}";
        }
        
        bool datasaving = ConfigService.Read("datasaving", false);
        if (datasaving)
        {
            arguments = arguments + " -sharebandwidth 0";
        }
        
        // 二次校验
        if (!File.Exists(CoreFilePath) || !new[] { fileMd5, Md532Bit }.Contains(GetFileMD5Hash(CoreFilePath)))
        {
            log("二次校验失败,进程已终止");
            await stopp2p();
            return false;
        }
        
        string? actualMd5 = GetFileMD5Hash(CoreFilePath);
        log($"{(actualMd5 == fileMd5 ? "64" : "32")}位核心校验通过，二次校验成功");
        
        // 启动进程
        if (await StartProcessAsync(arguments))
        {
            CoreStarted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 加入模式 - 加入联机房间（紧密衔接原Joiner_Click逻辑）
    /// </summary>
    public async Task<bool> StartJoinAsync(string roomCode, string targetPort)
    {
        role = "2"; //加入
        
        // DST模式检查
        bool EnableDST = ConfigService.Read("EnableDST", false);
        if (EnableDST)
        {
            // TODO: DSTJoin();
            return false;
        }
        
        user = roomCode;
        port = targetPort;
        
        // 检查核心是否已在运行 - 参考原WinForm逻辑，仅提示不阻止
        if (Process.GetProcessesByName("main").Length > 0)
        {
            AlreadyCore();
        }
        
        // 验证输入
        if (string.IsNullOrEmpty(port))
        {
            if (string.IsNullOrEmpty(user))
            {
                log("port与user无赋值内容");
                return false;
            }
            else
            {
                log("port无赋值内容");
                return false;
            }
        }
        else if (string.IsNullOrEmpty(user))
        {
            log("user无赋值内容");
            return false;
        }
        else
        {
            log("User&Port OK");
        }
        
        // 二次校验端口合法性
        if (!int.TryParse(port, out int p) || p < 1 || p > 65535)
        {
            log("端口不合法");
            return false;
        }
        
        // 获取默认Node
        string localName = Environment.MachineName;
        Random random = new Random();
        string randomPort = random.Next(1, 65536).ToString();
        
        bool usecustomport = ConfigService.Read("usecustomport", false);
        if (usecustomport)
        {
            string customport = ConfigService.Read("customport", "None");
            if (!int.TryParse(customport, out int parsedPort) || parsedPort < 1 || parsedPort > 65535)
            {
                log($"自定义端口无效: {customport}");
                return false;
            }
            else
            {
                randomPort = customport;
            }
        }
        
        // 设置目录
        Directory.CreateDirectory(CoreDirectory);
        Directory.SetCurrentDirectory(CoreDirectory);
        
        // 清理运行垃圾
        if (File.Exists("config.json")) File.Delete("config.json");
        if (File.Exists("config.json0")) File.Delete("config.json0");
        
        // 显示增强提醒
        string joinAddress = $"127.0.0.1:{randomPort}";
        log($"您的加入地址为 {joinAddress}");
        
        // 确定下载URL
        string url;
        string fileMd5;
        if (Environment.Is64BitOperatingSystem)
        {
            if (admin)
            {
                url = "https://api.mct.mczlf.loft.games/main32413.exe";
                fileMd5 = Md5Admin;
            }
            else
            {
                url = "https://api.mct.mczlf.loft.games/mainnew.exe";
                fileMd5 = Md5Normal;
            }
        }
        else
        {
            url = "https://api.mct.mczlf.loft.games/mainnew32.exe";
            fileMd5 = Md532Bit;
        }
        
        // 检查并下载核心
        bool needsDownload = false;
        if (File.Exists(CoreFilePath))
        {
            string? md5Hash = GetFileMD5Hash(CoreFilePath);
            if (md5Hash == fileMd5)
            {
                log("64位核心已存在且安全校验通过");
            }
            else if (md5Hash == Md532Bit)
            {
                log("32位核心已存在且安全校验通过");
            }
            else
            {
                log("核心不存在或安全校验不通过,重新Download中");
                needsDownload = true;
            }
        }
        else
        {
            log("核心不存在或安全校验不通过,重新Download中");
            needsDownload = true;
        }
        
        if (needsDownload)
        {
            ProgressChanged?.Invoke(this, 0); // 开始下载，进度归零
            log("Downloader启动");

            bool downloadSuccess = await DownloadCoreAsync(url, CoreFilePath);

            ProgressChanged?.Invoke(this, 0); // 下载结束，进度归零

            if (!downloadSuccess)
            {
                role = "0";
                return false;
            }
        }
        
        // 构造启动参数 - Join模式使用 -node -appname -peernode 格式
        string arguments;
        
        // 检查是否启用Relay
        bool EnableRelay = ConfigService.Read("EnableRelay", false);
        if (EnableRelay)
        {
            string RelayServer = ConfigService.Read("Server", "None");
            log($"已启用Relay,检索到_{RelayServer}");
            if (RelayServer == "None")
            {
                arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -token {tokenNormal}";
                ConfigService.Delete("Server");
            }
            else
            {
                arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -relaynode {RelayServer} -token {tokenNormal}";
            }
        }
        else
        {
            arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -token {tokenNormal}";
        }
        
        // TCP/UDP区分
        if (TCP)
        {
            arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -token {tokenNormal}";
        }
        else
        {
            arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -protocol udp -token {tokenTest}";
        }
        
        bool datasaving = ConfigService.Read("datasaving", false);
        if (datasaving)
        {
            arguments = arguments + " -sharebandwidth 0";
        }
        
        // 二次校验
        if (!File.Exists(CoreFilePath) || !new[] { fileMd5, Md532Bit }.Contains(GetFileMD5Hash(CoreFilePath)))
        {
            log("二次校验失败,进程已终止");
            await stopp2p();
            return false;
        }
        log($"{(GetFileMD5Hash(CoreFilePath) == fileMd5 ? "64" : "32")}位核心校验通过，二次校验成功");
        
        // 启动进程
        if (await StartProcessAsync(arguments))
        {
            CoreStarted?.Invoke(this, EventArgs.Empty);
            
            // 加入方：显示加入地址在InfoBar中 - 参考原WinForm的 infobutton.Text = $"127.0.0.1:{randomPort}";
            string displayAddress = $"127.0.0.1:{randomPort}";
            TsmGenerated?.Invoke(this, displayAddress);
            
            // 启动多播服务（如果启用）
            if (EnableServerPost)
            {
                try
                {
                    log("房间多播进程启动");
                    global::MinecraftConnectTool.Server_Post.Start_Post(int.Parse(randomPort));
                }
                catch (Exception ex)
                {
                    log($"多播发送线程异常：{ex.Message}");
                }
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 敏感词检测
    /// </summary>
    private async Task<bool> CheckProfanityAsync(string text)
    {
        try
        {
            using var client = new HttpClient();
            var content = new StringContent(
                JsonSerializer.Serialize(new { text }),
                Encoding.UTF8,
                "application/json");
            
            var response = await client.PostAsync(
                "https://uapis.cn/api/v1/text/profanitycheck",
                content);
            
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonNode.Parse(jsonString);
            
            if (result?["status"]?.GetValue<string>() == "forbidden")
            {
                var forbiddenWords = result?["forbidden_words"]?.AsArray();
                string words = forbiddenWords != null && forbiddenWords.Count > 0
                    ? string.Join(" | ", forbiddenWords.Select(w => w?.ToString()?.Trim() ?? "").Where(s => !string.IsNullOrEmpty(s)))
                    : "未知";
                
                log($"自定义内容存在敏感词 [{words}]");
                ConfigService.Delete("usecustomnode");
                ConfigService.Delete("customnode");
                return false;
            }
            
            return true;
        }
        catch (HttpRequestException ex)
        {
            log($"敏感词检测失败: {ex.Message}");
            ConfigService.Delete("usecustomnode");
            ConfigService.Delete("customnode");
            return false;
        }
        catch (Exception ex)
        {
            log($"敏感词检测失败: {ex.Message}");
            ConfigService.Delete("usecustomnode");
            ConfigService.Delete("customnode");
            return false;
        }
    }
    
    /// <summary>
    /// 获取文件MD5哈希
    /// </summary>
    public string? GetFileMD5Hash(string filePath)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// 下载核心文件
    /// </summary>
    private async Task<bool> DownloadCoreAsync(string url, string filePath)
    {
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            long downloadedBytes = 0;

            using var httpStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true);

            byte[] buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await httpStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    double progress = (double)downloadedBytes / totalBytes * 100;
                    ProgressChanged?.Invoke(this, progress);
                }
                else
                {
                    // 如果不知道总大小，显示一个循环进度（0-99循环）
                    double progress = (downloadedBytes % (100 * 1024)) / (100.0 * 1024) * 100;
                    ProgressChanged?.Invoke(this, progress);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            log($"下载核心失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 启动进程
    /// </summary>
    private Task<bool> StartProcessAsync(string arguments)
    {
        try
        {
            if (useoldway)
            {
                // 旧版方式：显示窗口
                Process.Start(new ProcessStartInfo()
                {
                    FileName = CoreFilePath,
                    Arguments = arguments,
                    UseShellExecute = true
                });
            }
            else
            {
                // 新版方式：重定向输出
                _currentProcess = new Process();
                _currentProcess.StartInfo.FileName = CoreFilePath;
                _currentProcess.StartInfo.Arguments = arguments;
                _currentProcess.StartInfo.UseShellExecute = false;
                _currentProcess.StartInfo.RedirectStandardOutput = true;
                _currentProcess.StartInfo.RedirectStandardError = true;
                _currentProcess.StartInfo.CreateNoWindow = true;
                _currentProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                _currentProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                
                _currentProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        log(e.Data);
                    }
                };
                
                _currentProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        log(e.Data);
                    }
                };
                
                _currentProcess.Start();
                _currentProcess.BeginOutputReadLine();
                _currentProcess.BeginErrorReadLine();
            }
            
            log("已尝试启动进程~");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            log($"启动进程失败: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 停止P2P核心
    /// </summary>
    public Task<bool> stopp2p()
    {
        try
        {
            // 停止多播服务
            try
            {
                global::MinecraftConnectTool.Server_Post.Stop_Post();
                log("多播服务已停止");
            }
            catch (Exception ex)
            {
                log($"停止多播服务异常: {ex.Message}");
            }
            
            // 停止当前进程
            if (_currentProcess != null && !_currentProcess.HasExited)
            {
                _currentProcess.Kill();
                _currentProcess.Dispose();
                _currentProcess = null;
            }
            
            // 停止所有main进程
            foreach (var process in Process.GetProcessesByName("main"))
            {
                process.Kill();
                process.Dispose();
            }
            
            role = "0";
            CoreStopped?.Invoke(this, EventArgs.Empty);
            log("P2P核心已停止");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            log($"停止P2P核心失败: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 核心已在运行提示
    /// </summary>
    private void AlreadyCore()
    {
        log("P2P核心已在运行中，请勿重复启动");
    }

    #region 工具方法

    /// <summary>
    /// Base64解码
    /// </summary>
    public static string? Base64Decode(string base64EncodedData)
    {
        try
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 读取Peer配置（从config.json恢复上次联机配置）
    /// </summary>
    public void ReadPeerConfig()
    {
        try
        {
            string configPath = Path.Combine(CoreDirectory, "config.json");
            if (!File.Exists(configPath))
            {
                return;
            }

            string json = File.ReadAllText(configPath, Encoding.UTF8);
            var root = JsonNode.Parse(json);
            var apps = root?["apps"]?.AsArray();
            var app = apps?.Count > 0 ? apps[0] : null;

            if (app == null) return;

            string? peerNode = app["PeerNode"]?.GetValue<string>();
            int? dstPort = app["DstPort"]?.GetValue<int>();

            if (!string.IsNullOrWhiteSpace(peerNode) && dstPort.HasValue)
            {
                // 检查是否是有效格式（以FO/HH/DD结尾）
                if (!peerNode.TrimEnd().EndsWith("FO") && 
                    !peerNode.TrimEnd().EndsWith("HH") && 
                    !peerNode.TrimEnd().EndsWith("DD"))
                {
                    return;
                }

                port = dstPort.Value.ToString();
                user = peerNode;
                log($"已读取到上次的联机配置,提示码{user}|端口{port}");
                
                // 触发配置恢复事件
                ConfigRestored?.Invoke(this, (user, port));
            }
        }
        catch (Exception ex)
        {
            try 
            { 
                File.AppendAllText(Path.Combine(CoreDirectory, "APPLog.ini"), $"ReadPeerConfig 异常：{ex}"); 
            } 
            catch { }
        }
    }

    /// <summary>
    /// 云公告获取
    /// </summary>
    public async Task<string?> CloudAlertAsync()
    {
        string url = "https://api.mct.mczlf.loft.games/cloudalert";
        string cacheFileName = Path.Combine(CoreDirectory, "MCZLFAPP_Temp_CloudAlertCache.flag");
        
        try
        {
            using var client = new HttpClient();
            string cloudalert = await client.GetStringAsync(url);
            log("📢" + cloudalert);
            
            // 检查内容变化
            string lastInfo = File.Exists(cacheFileName) ? File.ReadAllText(cacheFileName) : "";
            
            if (cloudalert.Contains("[info]", StringComparison.OrdinalIgnoreCase))
            {
                if (lastInfo != cloudalert)
                {
                    File.WriteAllText(cacheFileName, cloudalert);
                    CloudAlertReceived?.Invoke(this, ("info", cloudalert.Replace("[info]", "").Trim()));
                }
            }
            else if (cloudalert.Contains("[warn]", StringComparison.OrdinalIgnoreCase))
            {
                CloudAlertReceived?.Invoke(this, ("warn", cloudalert.Replace("[warn]", "").Trim()));
            }
            else if (cloudalert.Contains("[error]", StringComparison.OrdinalIgnoreCase))
            {
                CloudAlertReceived?.Invoke(this, ("error", cloudalert.Replace("[error]", "").Trim()));
            }
            else if (cloudalert.Contains("[alerterror]", StringComparison.OrdinalIgnoreCase))
            {
                CloudAlertReceived?.Invoke(this, ("alerterror", cloudalert.Replace("[alerterror]", "").Trim()));
            }
            else if (cloudalert.Contains("[alertwarn]", StringComparison.OrdinalIgnoreCase))
            {
                CloudAlertReceived?.Invoke(this, ("alertwarn", cloudalert.Replace("[alertwarn]", "").Trim()));
            }
            
            return cloudalert;
        }
        catch (Exception ex)
        {
            log("获取云公告内容失败：" + ex.Message);
            return null;
        }
    }

    #endregion

    #region 新增事件

    /// <summary>
    /// 配置恢复事件 - 恢复上次联机配置
    /// </summary>
    public event EventHandler<(string User, string Port)>? ConfigRestored;

    /// <summary>
    /// 云公告接收事件
    /// </summary>
    public event EventHandler<(string Type, string Message)>? CloudAlertReceived;

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // 停止P2P进程
        try
        {
            if (_currentProcess != null)
            {
                if (!_currentProcess.HasExited)
                {
                    _currentProcess.Kill();
                }
                _currentProcess.Dispose();
                _currentProcess = null;
            }
        }
        catch { }

        // 停止多播服务
        try
        {
            global::MinecraftConnectTool.Server_Post.Stop_Post();
        }
        catch { }

        // 清除所有事件订阅
        LogMessage = null;
        ProgressChanged = null;
        TsmGenerated = null;
        CoreStarted = null;
        CoreStopped = null;
        ConfigRestored = null;
        NatTypeDetected = null;
        StatusChanged = null;
        ErrorOccurred = null;
        CloudAlertReceived = null;

        GC.SuppressFinalize(this);
    }

    #endregion
}
