using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.ViewModels.Pages;

public enum NatType
{
    Unknown,
    OpenInternet,
    FullCone,
    RestrictedCone,
    PortRestrictedCone,
    Symmetric,
    SymmetricUdpFirewall,
    Blocked
}

public partial class OptimizePageViewModel : ViewModelBase
{
    // 主要STUN服务器
    private readonly string[] _primaryStunServers = new[]
    {
        "stun.cloudflare.com:3478",
        "stun.l.google.com:19302",
        "stun1.l.google.com:19302",
        "stun2.l.google.com:19302"
    };

    // 备用STUN服务器池
    private readonly string[] _backupStunServers = new[]
    {
        "stun.geesthacht.de:3478",
        "stun.mixvoip.com:3478",
        "stun.lovense.com:3478",
        "stun.atagverwarming.nl:3478",
        "stun.sip.us:3478",
        "stun.annatel.net:3478",
        "stun.verbo.be:3478",
        "stun.ncic.com:3478",
        "stun.bcs2005.net:3478",
        "stun.oncloud7.ch:3478",
        "stun.poetamatusel.org:3478",
        "stun.voipia.net:3478",
        "stun.kaseya.com:3478",
        "stun.ringostat.com:3478",
        "stun.3deluxe.de:3478",
        "stun.nextcloud.com:3478",
        "stun.frozenmountain.com:3478",
        "stun.threema.ch:3478",
        "stun.genymotion.com:3478",
        "stun.framasoft.org:3478",
        "stun.bethesda.net:3478",
        "stun.sonetel.com:3478",
        "stun.vomessen.de:3478",
        "stun.cope.es:3478",
        "stun.acronis.com:3478",
        "stun.thinkrosystem.com:3478",
        "stun.signalwire.com:3478",
        "stun.business-isp.nl:3478",
        "stun.fitauto.ru:3478",
        "stun.pure-ip.com:3478",
        "stun.moonlight-stream.org:3478",
        "stun.stochastix.de:3478",
        "stun.linuxtrent.it:3478",
        "stun.meetwife.com:3478",
        "stun.antisip.com:3478",
        "stun.ukh.de:3478",
        "stun.bridesbay.com:3478",
        "stun.finsterwalder.com:3478",
        "stun.voztovoice.org:3478",
        "stun.skydrone.aero:3478",
        "stun.technosens.fr:3478",
        "stun.siplogin.de:3478",
        "stun.tula.nu:3478",
        "stun.romancecompass.com:3478",
        "stun.sipthor.net:3478",
        "stun.telviva.com:3478",
        "stun.engineeredarts.co.uk:3478",
        "stun.sonetel.net:3478",
        "stun.telnyx.com:3478",
        "stun.siptrunk.com:3478",
        "stun.ttmath.org:3478",
        "stun.voipgate.com:3478",
        "stun.f.haeder.net:3478",
        "stun.baltmannsweiler.de:3478",
        "stun.ru-brides.com:3478",
        "stun.uabrides.com:3478",
        "stun.graftlab.com:3478",
        "stun.ipfire.org:3478",
        "stun.myspeciality.com:3478",
        "stun.peethultra.be:3478",
        "stun.fmo.de:3478",
        "stun.healthtap.com:3478",
        "stun.axialys.net:3478",
        "stun.alpirsbacher.de:3478",
        "stun.zentauron.de:3478",
        "stun.files.fm:3478",
        "stun.hot-chilli.net:3478",
        "stun.freeswitch.org:3478",
        "stun.m-online.net:3478",
        "stun.godatenow.com:3478",
        "stun.voip.blackberry.com:3478",
        "stun.diallog.com:3478",
        "stun.cellmail.com:3478",
        "stun.flashdance.cx:3478",
        "stun.dcalling.de:3478",
        "stun.romaaeterna.nl:3478",
        "stun.radiojar.com:3478",
        "stun.bitburger.de:3478",
        "stun.kanojo.de:3478"
    };

    [ObservableProperty]
    private string _natTypeText = "未检测";

    [ObservableProperty]
    private string _natTypeDescription = "点击检测按钮开始检测NAT类型";

    [ObservableProperty]
    private string _p2pSuggestion = "";

    [ObservableProperty]
    private bool _p2pSuggestionSuccess;

    [ObservableProperty]
    private bool _p2pSuggestionWarning;

    [ObservableProperty]
    private bool _p2pSuggestionError;

    [ObservableProperty]
    private string _publicIp = "--";

    [ObservableProperty]
    private string _publicPort = "--";

    [ObservableProperty]
    private bool _isDetecting;

    [ObservableProperty]
    private string _detectButtonText = "开始检测";

    [ObservableProperty]
    private string _detectButtonIcon = "PlaySpeed";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _detailedLog = "";

    private readonly StringBuilder _logBuilder = new();

    [RelayCommand]
    private async Task DetectNatType()
    {
        IsDetecting = true;
        DetectButtonText = "检测中...";
        DetectButtonIcon = "Loading";
        _logBuilder.Clear();
        DetailedLog = "";
        HasError = false;

        Log("=== NAT类型检测开始 ===");
        Log($"检测时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log("");

        try
        {
            var result = await Task.Run(() => PerformCompleteStunTest());

            NatTypeText = result.Type switch
            {
                NatType.OpenInternet => "开放网络 (Open Internet)",
                NatType.FullCone => "全锥型NAT (Full Cone)",
                NatType.RestrictedCone => "限制锥型NAT (Restricted Cone)",
                NatType.PortRestrictedCone => "端口限制锥型NAT (Port Restricted Cone)",
                NatType.Symmetric => "对称型NAT (Symmetric)",
                NatType.SymmetricUdpFirewall => "对称UDP防火墙 (Symmetric UDP Firewall)",
                NatType.Blocked => "UDP被阻断 (Blocked)",
                _ => "未知"
            };

            NatTypeDescription = result.Type switch
            {
                NatType.OpenInternet => "您的网络环境非常好，可以直接进行P2P连接，无需任何优化。",
                NatType.FullCone => "您的NAT类型良好，P2P连接成功率很高。",
                NatType.RestrictedCone => "您的NAT类型一般，大部分P2P连接可以成功。",
                NatType.PortRestrictedCone => "您的NAT类型较差，可能会影响P2P连接成功率。建议开启UPnP或使用中继服务器。",
                NatType.Symmetric => "您的NAT类型为对称型，P2P连接可能会失败。建议开启UPnP或使用中继服务器。",
                NatType.SymmetricUdpFirewall => "您的网络限制了UDP通信，P2P连接可能会失败。",
                NatType.Blocked => "UDP通信被完全阻断，无法进行P2P连接。请检查防火墙设置。",
                _ => "无法确定NAT类型"
            };

            // 设置P2P联机建议
            (P2pSuggestion, P2pSuggestionSuccess, P2pSuggestionWarning, P2pSuggestionError) = result.Type switch
            {
                NatType.OpenInternet => ("✓ 您的网络可以进行P2P联机，连接成功率极高", true, false, false),
                NatType.FullCone => ("✓ 您的网络可以进行P2P联机，连接成功率很高", true, false, false),
                NatType.RestrictedCone => ("✓ 您的网络可以进行P2P联机，但部分用户可能无法连接", true, false, false),
                NatType.PortRestrictedCone => ("⚠ 您的网络可以进行P2P联机，但可能成功率较低，建议开启UPnP，或使用Link模式", false, true, false),
                NatType.Symmetric => ("✗ 您的网络可能无法正常进行P2P联机，建议开启UPnp\n或使用Link模式", false, false, true),
                NatType.SymmetricUdpFirewall => ("✗ 您的网络无法进行P2P联机，请检查防火墙设置，或使用Link模式", false, false, true),
                NatType.Blocked => ("✗ 您的网络无法进行P2P联机，UDP通信被阻断，请使用Link模式", false, false, true),
                _ => ("", false, false, false)
            };

            PublicIp = result.PublicIp ?? "--";
            PublicPort = result.PublicPort > 0 ? result.PublicPort.ToString() : "--";
            
            Log("");
            Log("=== 检测结果 ===");
            Log($"NAT类型: {NatTypeText}");
            Log($"公网IP: {PublicIp}");
            Log($"公网端口: {PublicPort}");
            Log("=== 检测结束 ===");

            StatusMessage = result.Type == NatType.Blocked 
                ? "检测完成 - UDP通信可能被阻断" 
                : "检测完成";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"检测失败: {ex.Message}";
            NatTypeText = "检测失败";
            NatTypeDescription = "请检查网络连接后重试";
            Log($"错误: {ex.Message}");
            Log($"堆栈: {ex.StackTrace}");
        }
        finally
        {
            IsDetecting = false;
            DetectButtonText = "开始检测";
            DetectButtonIcon = "PlaySpeed";
        }
    }

    private void Log(string message)
    {
        _logBuilder.AppendLine(message);
        DetailedLog = _logBuilder.ToString();
    }

    [RelayCommand]
    private async Task CopyLog()
    {
        try
        {
            var success = await ClipboardHelper.SetTextAsync(DetailedLog);
            StatusMessage = success ? "日志已复制到剪贴板" : "复制失败";
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败: {ex.Message}";
        }
    }

    private StunResult PerformCompleteStunTest()
    {
        // 测试1: 基本连通性测试（使用主要服务器）
        Log("[测试1] 基本连通性测试...");
        var result = TestBasicConnectivity();
        if (result.Type == NatType.Blocked)
        {
            Log("结果: UDP通信被阻断");
            return result;
        }
        Log($"结果: 连通性正常，公网IP={result.PublicIp}, 端口={result.PublicPort}");
        Log("");

        // 测试2: 检查是否为开放网络
        Log("[测试2] 开放网络检测...");
        if (TestOpenInternet(result))
        {
            Log("结果: 开放网络 (Open Internet)");
            result.Type = NatType.OpenInternet;
            return result;
        }
        Log("结果: 不是开放网络，继续检测...");
        Log("");

        // 测试3: 备用服务器测试（测试15个可连接的服务器）
        Log("[测试3] 多服务器映射一致性测试...");
        var backupResults = TestBackupServers(15);
        Log($"成功连接 {backupResults.Count} 个备用服务器");
        
        foreach (var (server, ip, port) in backupResults)
        {
            Log($"  - {server}: {ip}:{port}");
        }
        Log("");

        // 分析映射结果判断NAT类型
        var natTypeFromMapping = AnalyzeNatTypeFromMapping(result, backupResults);
        result.Type = natTypeFromMapping;
        return result;
    }

    /// <summary>
    /// 根据多个STUN服务器的映射结果分析NAT类型
    /// </summary>
    private NatType AnalyzeNatTypeFromMapping(StunResult primaryResult, List<(string server, string ip, int port)> backupResults)
    {
        // 如果没有足够的备用服务器结果，无法准确判断
        if (backupResults.Count < 3)
        {
            Log("  备用服务器连接不足，使用基础判断");
            // 回退到基础判断
            if (TestPortRestricted(primaryResult))
                return NatType.PortRestrictedCone;
            return NatType.RestrictedCone;
        }

        // 统计映射情况
        var allMappings = new List<(string ip, int port)>();
        allMappings.Add((primaryResult.PublicIp ?? "", primaryResult.PublicPort));
        foreach (var (_, ip, port) in backupResults)
        {
            allMappings.Add((ip, port));
        }

        // 统计唯一映射数量
        var uniqueMappings = new HashSet<string>();
        foreach (var (ip, port) in allMappings)
        {
            if (!string.IsNullOrEmpty(ip) && port > 0)
            {
                uniqueMappings.Add($"{ip}:{port}");
            }
        }

        Log($"  总映射数: {allMappings.Count}, 唯一映射数: {uniqueMappings.Count}");

        // 判断逻辑：
        // 1. 如果所有映射都相同 -> 可能是全锥型或限制锥型
        // 2. 如果有多个不同映射 -> 对称型NAT

        if (uniqueMappings.Count == 1)
        {
            // 所有服务器返回相同的映射
            // 检查是否为全锥型（与主要结果比较）
            var firstMapping = allMappings[0];
            int sameCount = allMappings.Count(m => m.ip == firstMapping.ip && m.port == firstMapping.port);
            
            Log($"  所有映射相同，相同映射比例: {sameCount}/{allMappings.Count}");
            
            // 如果与主要服务器映射相同，可能是全锥型
            // 但需要进一步测试确认，这里保守判断为限制锥型
            // 因为真正的全锥型需要额外的测试来验证
            Log("结果: 限制锥型NAT (Restricted Cone) - 所有映射一致");
            return NatType.RestrictedCone;
        }
        else
        {
            // 有多个不同的映射，说明是对称型NAT
            Log($"  检测到 {uniqueMappings.Count} 个不同的映射");
            Log("结果: 对称型NAT (Symmetric)");
            return NatType.Symmetric;
        }
    }

    private StunResult TestBasicConnectivity()
    {
        using var udpClient = new UdpClient(AddressFamily.InterNetwork);
        udpClient.Client.ReceiveTimeout = 5000;
        udpClient.Client.SendTimeout = 5000;

        foreach (var server in _primaryStunServers)
        {
            try
            {
                var parts = server.Split(':');
                var host = parts[0];
                var port = int.Parse(parts[1]);

                Log($"  尝试连接: {host}:{port}");
                var addresses = Dns.GetHostAddresses(host);
                // 只使用IPv4地址
                var ipv4Address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4Address == null)
                {
                    Log($"  未找到IPv4地址");
                    continue;
                }

                var serverEndPoint = new IPEndPoint(ipv4Address, port);
                var request = CreateStunBindingRequest();
                udpClient.Send(request, request.Length, serverEndPoint);

                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var response = udpClient.Receive(ref remoteEndPoint);

                Log($"  成功收到响应 from {host}");
                return ParseStunResponse(response);
            }
            catch (Exception ex)
            {
                Log($"  连接失败: {ex.Message}");
                continue;
            }
        }

        return new StunResult { Type = NatType.Blocked };
    }

    private List<(string server, string ip, int port)> TestBackupServers(int targetCount)
    {
        var results = new List<(string, string, int)>();
        var random = new Random();
        // 随机打乱备用服务器列表
        var shuffledServers = _backupStunServers.OrderBy(x => random.Next()).ToList();

        using var udpClient = new UdpClient(AddressFamily.InterNetwork);
        udpClient.Client.ReceiveTimeout = 3000;
        udpClient.Client.SendTimeout = 3000;

        // 遍历服务器列表，直到获得足够的成功结果或遍历完所有服务器
        foreach (var server in shuffledServers)
        {
            // 如果已经获得足够的结果，提前退出
            if (results.Count >= targetCount)
                break;

            try
            {
                var parts = server.Split(':');
                var host = parts[0];
                var port = int.Parse(parts[1]);

                var addresses = Dns.GetHostAddresses(host);
                // 只使用IPv4地址
                var ipv4Address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4Address == null) continue;

                var serverEndPoint = new IPEndPoint(ipv4Address, port);
                var request = CreateStunBindingRequest();
                udpClient.Send(request, request.Length, serverEndPoint);

                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var response = udpClient.Receive(ref remoteEndPoint);
                var result = ParseStunResponse(response);

                if (!string.IsNullOrEmpty(result.PublicIp) && result.PublicPort > 0)
                {
                    results.Add((server, result.PublicIp, result.PublicPort));
                }
            }
            catch
            {
                // 跳过失败的，继续尝试下一个
                continue;
            }
        }

        return results;
    }

    private bool TestOpenInternet(StunResult result)
    {
        if (string.IsNullOrEmpty(result.PublicIp))
            return false;

        var ip = IPAddress.Parse(result.PublicIp);
        var bytes = ip.GetAddressBytes();

        // 检查是否为内网IP
        if (bytes[0] == 10) return false;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
        if (bytes[0] == 192 && bytes[1] == 168) return false;
        if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return false;
        if (bytes[0] == 127) return false;

        // 获取本地IP进行比较
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 53);
            var localEndPoint = socket.LocalEndPoint as IPEndPoint;
            if (localEndPoint != null && localEndPoint.Address.ToString() == result.PublicIp)
            {
                return true;
            }
        }
        catch { }

        return false;
    }
    #region 旧判断逻辑（折叠）
    //private bool TestFullCone(StunResult result, List<(string server, string ip, int port)> backupResults)
    //{
    //    if (string.IsNullOrEmpty(result.PublicIp) || result.PublicPort <= 0)
    //        return false;

    //    int sameMappingCount = 0;
    //    foreach (var (_, ip, port) in backupResults)
    //    {
    //        if (ip == result.PublicIp && port == result.PublicPort)
    //        {
    //            sameMappingCount++;
    //        }
    //    }

    //    Log($"  相同映射数量: {sameMappingCount}/{backupResults.Count}");
    //    // 如果超过一半的服务器返回相同的映射，认为是全锥型
    //    return backupResults.Count > 0 && sameMappingCount >= backupResults.Count / 2;
    //}

    //private bool TestSymmetric(StunResult result, List<(string server, string ip, int port)> backupResults)
    //{
    //    if (backupResults.Count < 2) return false;

    //    var uniqueMappings = new HashSet<string>();
    //    foreach (var (_, ip, port) in backupResults)
    //    {
    //        uniqueMappings.Add($"{ip}:{port}");
    //    }

    //    Log($"  唯一映射数量: {uniqueMappings.Count}");
    //    // 如果有多个不同的映射，认为是对称型NAT
    //    return uniqueMappings.Count > 1;
    //}
    #endregion
    private bool TestPortRestricted(StunResult result)
    {
        return !string.IsNullOrEmpty(result.PublicIp) && result.PublicPort > 0;
    }

    private byte[] CreateStunBindingRequest()
    {
        var message = new byte[20];

        // Message Type: Binding Request (0x0001)
        message[0] = 0x00;
        message[1] = 0x01;

        // Message Length: 0 (no attributes)
        message[2] = 0x00;
        message[3] = 0x00;

        // Magic Cookie (0x2112A442)
        message[4] = 0x21;
        message[5] = 0x12;
        message[6] = 0xA4;
        message[7] = 0x42;

        // Transaction ID (random 12 bytes)
        var random = new Random();
        for (int i = 8; i < 20; i++)
        {
            message[i] = (byte)random.Next(256);
        }

        return message;
    }

    private StunResult ParseStunResponse(byte[] response)
    {
        var result = new StunResult { Type = NatType.Unknown };

        if (response.Length < 20)
            return result;

        // 检查Magic Cookie
        if (response[4] != 0x21 || response[5] != 0x12 ||
            response[6] != 0xA4 || response[7] != 0x42)
        {
            return result;
        }

        // 解析属性
        int pos = 20;
        while (pos + 4 <= response.Length)
        {
            ushort attrType = (ushort)((response[pos] << 8) | response[pos + 1]);
            ushort attrLength = (ushort)((response[pos + 2] << 8) | response[pos + 3]);

            int paddedLength = (attrLength + 3) & ~3;

            if (pos + 4 + paddedLength > response.Length)
                break;

            // XOR-MAPPED-ADDRESS (0x0020) 或 MAPPED-ADDRESS (0x0001)
            if (attrType == 0x0020 || attrType == 0x0001)
            {
                ParseAddressAttribute(response, pos + 4, attrLength, attrType == 0x0020, result);
            }

            pos += 4 + paddedLength;
        }

        return result;
    }

    private void ParseAddressAttribute(byte[] data, int offset, int length, bool isXor, StunResult result)
    {
        if (length < 4)
            return;

        byte family = data[offset + 1];

        if (family == 0x01 && length >= 8) // IPv4
        {
            ushort port;
            if (isXor)
            {
                port = (ushort)(((data[offset + 2] << 8) | data[offset + 3]) ^ 0x2112);
                result.PublicIp = $"{(data[offset + 4] ^ 0x21)}.{(data[offset + 5] ^ 0x12)}.{(data[offset + 6] ^ 0xA4)}.{(data[offset + 7] ^ 0x42)}";
            }
            else
            {
                port = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
                result.PublicIp = $"{data[offset + 4]}.{data[offset + 5]}.{data[offset + 6]}.{data[offset + 7]}";
            }
            result.PublicPort = port;
        }
    }

    private class StunResult
    {
        public NatType Type { get; set; }
        public string? PublicIp { get; set; }
        public int PublicPort { get; set; }
    }
}
