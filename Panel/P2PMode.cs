using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using System.Threading;
using System.Linq;

namespace MinecraftConnectTool
{
    public partial class P2PMode : UserControl
    {
        public P2PMode()
        {
            InitializeComponent();
            materialSingleLineTextField2.LostFocus += new EventHandler(materialSingleLineTextField2_LostFocus);
            materialSingleLineTextField3.LostFocus += new EventHandler(materialSingleLineTextField3_LostFocus);
            alert1.Visible = false;
            infobutton.Visible = false;
        }
        public string tokenNormal = "17073157824633806511";
        public string tokenTest = "7196174974940052261";
        public static Font P2PFont { get; } = new Font("Microsoft YaHei UI", 9f);
        public string role = "0"; //0=未开启  1=房主 2=加入方
        private readonly List<object> _floatButtons = new List<object>();
        public static Font AlertFont { get; } = new Font("Microsoft YaHei UI", 8.3f);
        private string version;
        bool useoldway = false;
        bool EnableServerPost = Form1.config.read<bool>("ServerPostEnable", true);
        bool hasreadofflinemessage = false;
        string arguments;
        string tsm;
        bool TCP = Form1.config.read<bool>("TCP", true);
        //private TcpPing _ping;
        private async void Opener_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(new[] { "", "每次", "每小时", "每天", "永久" }[Form1.config.read<int>("codeupdate", 1) is var v && v >= 1 && v <= 4 ? v : 0]);
            role = "1"; //房主
            if (Process.GetProcessesByName("main").Length > 0) AlreadyCore();
            badge3.Visible = true; badge3.State = TState.Processing; badge3.Text = "正在处理中...";
            log("检查P2PMode核心中..");
            {
                string localName = Environment.MachineName;
                Random random = new Random();
                int randomPort = random.Next(1, 65536);
                string tempDirectory = Path.GetTempPath();
                string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
                Directory.CreateDirectory(customDirectory);
                Directory.SetCurrentDirectory(customDirectory);
                //清理运行垃圾
                if (File.Exists("config.json")) File.Delete("config.json");
                if (File.Exists("config.json0")) File.Delete("config.json0");
                //增强提醒
                int codeupate = Form1.config.read<int>("codeupdate", 1);//自定义提示码更新时间
                if (codeupate == 1)
                { tsm = $"{localName}{randomPort}"; }
                else if (codeupate == 2) { tsm = $"{localName}{DateTime.Now:yyyyMMddHH}H"; } else if (codeupate == 3) { tsm = $"{localName}{DateTime.Now:yyyyMMdd}D"; }
                else if (codeupate == 4) //永久
                {
                    if (Form1.config.read<bool>("usecustomnode", false))
                    {
                        tsm = Form1.config.read<string>("customnode");
                        try
                        {
                            var j = await new System.Net.Http.HttpClient().GetStringAsync(
                                $"https://uapis.cn/api/prohibited?text={Uri.EscapeDataString(tsm)}");
                            var w = Newtonsoft.Json.Linq.JObject.Parse(j)["forbiddenWord"]?.ToString();
                            if (!string.IsNullOrEmpty(w))
                            {
                                Program.alerterror("自定义内容存在敏感词,请整改后重试\n已删除相关配置,如需重新开启请在 设置>提示码固定\n如您未修改过相关设置,请尝试清空缓存");
                                stopp2p();
                                Form1.config.delete("usecustomnode");
                                Form1.config.delete("customnode");
                                return;
                            }
                        }
                        catch
                        {
                            Form1.config.delete("usecustomnode");
                            Form1.config.delete("customnode");
                            Program.alerterror("敏感词检测失败,请检查网络是否正常\n已删除相关配置,如需重新开启请在 设置>提示码固定\n如您未修改过相关设置,请尝试清空缓存");
                            stopp2p();
                            return;
                        }
                    }
                }
                else tsm = $"{localName}{randomPort}";

                if (tsm.Length <= 8)
                {
                    int threeDigitRandom = random.Next(100, 1000);
                    tsm += threeDigitRandom.ToString(); // 将三位数随机数追加到tsm后面
                    log("提示码不满足要求,已自动加入三位随机数字");
                }
                //如果启用了兼容，就再加个头尾
                bool EnableOLAN = Form1.config.read<bool>("EnableOLAN", false);
                if (EnableOLAN)
                { tsm = "M" + tsm + "C"; }
                MessageBox.Show($"您的提示码为{tsm}\n已复制入剪切板中,快去粘贴给小伙伴吧", "增强提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AntdUI.Notification.info(Program.MainForm, "增强提醒", $"您的提示码为{tsm}\n已复制入剪切板中,快去粘贴给小伙伴吧", align: AntdUI.TAlignFrom.BR, font: P2PFont);
                //显示提示信息
                AntdUI.Message.info(Program.MainForm, $"单击屏幕增强提醒右侧的信息展示按钮,即可自动复制↓", autoClose: 5, font: P2PFont);
                alert1.Text = "提示码→";
                infobutton.Text = tsm;
                infobutton.Visible = true;
                alert1.Visible = true;
                //End
                //新旧版本区分用的admin
                string url;
                if (Environment.Is64BitOperatingSystem)
                {
                    if (admin)
                    { url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/main32413.exe"; }
                    else { url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew.exe"; }//new  
                }
                else
                {
                    url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew32.exe";
                }
                string fileName = Path.Combine(customDirectory, "main.exe");
                string fileMd5;
                if (admin) { fileMd5 = "29d76fc2626c66925621d475f3a6827a"; } else { fileMd5 = "08160296509deac13e7d12c8754de9ef"; };
                string fileMd532 = "640ffdaa2a7b249d9c301102419a69cb";
                bool needsDownload = false;
                if (File.Exists(fileName))
                {
                    string md5Hash = GetFileMD5Hash(fileName);
                    if (md5Hash == fileMd5)
                    {
                        log("64位核心已存在且安全校验通过");
                    }
                    else
                    {
                        if (md5Hash == fileMd532)
                        {
                            log("32位核心已存在且安全校验通过");
                            needsDownload = false;
                        }
                        else
                        {
                            if (md5Hash == null)
                            {
                                log("出现错误，进程终止");
                                role = "0";  //未开启或是重置状态
                                badge3.Visible = false; badge3.State = TState.Default; badge3.Text = "Null";
                                return;
                            }
                            else
                            {
                                log("核心不存在或安全校验不通过,重新Download中");
                                needsDownload = true;
                            }
                        }

                    }
                }
                else
                {
                    log("核心不存在或安全校验不通过,重新Download中");
                    needsDownload = true;
                }
                //Download
                if (needsDownload)
                {
                    progress1.ShowInTaskbar = true;
                    progress1.Visible = true;
                    progress1.Value = 0;
                    // 下载
                    log("Downloader启动");
                    using (var unityClient = new HttpClient())
                    using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        long totalBytes = response.Content.Headers.ContentLength ?? 0;
                        long downloadedBytes = 0;

                        using (var httpStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;

                            while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                downloadedBytes += bytesRead;

                                if (totalBytes > 0)
                                {
                                    int progress = (int)((downloadedBytes * 100) / totalBytes);
                                    progress1.Value = progress;
                                }
                            }
                        }
                    }
                    progress1.Visible = false;
                    progress1.Value = 0;
                    progress1.ShowInTaskbar = false;
                    log("Task:下载核心中..");
                }
                //清除状态
                progress1.Visible = false;
                progress1.Value = 0;
                progress1.ShowInTaskbar = false;
                log("构造启动参数中..");
                log("复制邀请信息中..");
                try
                {
                    Clipboard.SetText($"邀请你加入我的Minecraft联机房间！\n提示码为 {tsm}\n复制时请勿带上前面的中文哦");
                }
                catch (Exception ex)
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Oops,出了点小问题", "复制默认邀请信息时出错\n请在上方常驻信息栏或是右侧日志中查找到需要复制的提示码\n错误信息" + ex, AntdUI.TType.Error)
                    {
                        CloseIcon = true,
                        Font = AlertFont,
                        Draggable = false,
                        CancelText = null,
                        OkText = "好的"
                    });
                }
                log($"您的提示码为 {tsm}");
                // 构造启动参数
                string arguments;
                if (TCP)
                { arguments = $"-node {tsm} -token {tokenNormal}"; }
                else { arguments = $"-node {tsm} -protocol udp -token {tokenTest}"; }//UDP
                //二次校验
                //      log(fileMd5);
                if (!File.Exists(fileName) || !new[] { fileMd5, fileMd532 }.Contains(GetFileMD5Hash(fileName)))
                { log("二次校验失败,进程已终止"); stopp2p(); return; }
                log($"{(GetFileMD5Hash(fileName) == fileMd5 ? "64" : "32")}位核心校验通过，二次校验成功");
                //正常启动
                if (useoldway)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process process = new Process();
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false; // 关闭外壳程序以支持重定向
                    process.StartInfo.RedirectStandardOutput = true; // 重定向标准输出
                    process.StartInfo.RedirectStandardError = true; // 重定向错误输出
                    process.StartInfo.CreateNoWindow = true; // 不显示窗口

                    // 捕获输出并写入 RichTextBox
                    process.OutputDataReceived += (processSender, outputEventArgs) =>
                    {
                        if (!string.IsNullOrEmpty(outputEventArgs.Data))
                        {
                            log(outputEventArgs.Data); // 将输出内容记录到 RichTextBox
                        }
                    };

                    process.ErrorDataReceived += (processSender, errorEventArgs) =>
                    {
                        if (!string.IsNullOrEmpty(errorEventArgs.Data))
                        {
                            log(errorEventArgs.Data); // 将错误内容记录到 RichTextBox
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    AntdUI.Message.info(Program.MainForm, "已尝试启动进程,日志内容将输出至右侧哦", autoClose: 5, font: P2PFont);
                    log("已尝试启动进程~");
                    Opener.Badge = "运行中";
                    // 原来：floatButtonOpen = FloatButton.open(...)
                    // 借助先进的AI工具，修了一下SHIT山
                    CreateFloatButton(new FloatButton.Config(Program.MainForm, new[]{
                    new FloatButton.ConfigBtn("CloseButton", Properties.Resources.close){
                     Text = "关闭",
                    Tooltip = "关闭P2P核心-all",
                    Round = true,
                    Type = TTypeMini.Primary,
                    Radius = 6,
                    Enabled = true,
                    Loading = false
                        }}, _ => stopp2p())
                    {
                        Align = TAlign.BR,
                        Vertical = true,
                        TopMost = false,
                        MarginX = 24,
                        MarginY = 24,
                        Gap = 10,
                        Font = P2PFont
                    });
                    //log(arguments);
                }
            }
        }
        private async void Joiner_Click(object sender, EventArgs e)
        {
            role = "2";  //加入

            bool EnableDST = Form1.config.read<bool>("EnableDST", false);
            if (EnableDST) { DSTJoin(); return; }

            user = materialSingleLineTextField2.Text;
            port = materialSingleLineTextField3.Text;
            if (Process.GetProcessesByName("main").Length > 0) AlreadyCore();
            if (string.IsNullOrEmpty(port) || port.Trim().Equals("输入目标端口", StringComparison.OrdinalIgnoreCase))
            { if (string.IsNullOrEmpty(user) || user.Trim().Equals("输入提示码", StringComparison.OrdinalIgnoreCase))
                { AntdUI.Message.warn(Program.MainForm, "你好像没有填写或确认[提示码]和[端口]哦", autoClose: 5, font: P2PFont); log("port与user无赋值内容"); return; }
                else { AntdUI.Message.warn(Program.MainForm, "你好像没有填写或确认[端口]哦", autoClose: 5, font: P2PFont); log("port无赋值内容"); return; } }
            else if (string.IsNullOrEmpty(user) || user.Trim().Equals("输入提示码", StringComparison.OrdinalIgnoreCase))
            { AntdUI.Message.warn(Program.MainForm, "你好像没有填写或确认[提示码]哦", autoClose: 5, font: P2PFont); log("user无赋值内容"); return;
            } else { log("User&Port OK"); }
            //二次校验端口合法性
            if (!int.TryParse(port, out int p) || p < 1 || p > 65535)
            {
                log("端口不合法");
                AntdUI.Message.error(Program.MainForm, "[端口]是不是填错了...[1-65535]", autoClose: 5, font: P2PFont);
                return;
            }
            //正常启动
            //获取默认Node
            badge3.Visible = true; badge3.State = TState.Processing; badge3.Text = "正在处理中...";
            string localName = Environment.MachineName;
            Random random = new Random();
            string randomPort = random.Next(1, 65536).ToString(); // 生成随机端口号并转换为字符串
            bool usecustomport = Form1.config.read<bool>("usecustomport", false);
            if (usecustomport)
            {
                string customport = Form1.config.read<string>("customport", "None");
                if (!int.TryParse(customport, out int parsedPort) || parsedPort < 1 || parsedPort > 65535)
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Wow...意料之外呢", "线程小伙读取到了自定义端口信息,但是端口居然是神秘的字符\n读取到的自定义端口：" + customport, AntdUI.TType.Warn)
                    {
                        CloseIcon = true,
                        Font = P2PFont,
                        Draggable = false,
                        CancelText = null,
                        OkText = "好的"
                    });
                    return;
                }
                else
                {
                    randomPort = customport; // 将 customport 的字符串值赋值给 randomPort
                }
            }
            string tempDirectory = Path.GetTempPath();
            string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
            Directory.CreateDirectory(customDirectory);
            Directory.SetCurrentDirectory(customDirectory);
            //清理运行垃圾
            if (File.Exists("config.json")) File.Delete("config.json");
            if (File.Exists("config.json0")) File.Delete("config.json0");
            //增强提醒
            MessageBox.Show($"您的加入地址为127.0.0.1:{randomPort}\n已复制入剪切板中,快去和小伙伴一起玩吧", "增强提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AntdUI.Notification.info(Program.MainForm, "增强提醒", $"您的加入地址为127.0.0.1:{randomPort}\n已复制入剪切板中,快去和小伙伴一起玩吧", align: AntdUI.TAlignFrom.BR, font: P2PFont);
            //输出到常驻信息
            alert1.Text = "加入地址→";
            infobutton.Text = $"127.0.0.1:{randomPort}";
            alert1.Visible = true;
            infobutton.Visible = true;
            AntdUI.Message.info(Program.MainForm, $"单击屏幕增强提醒右侧的信息展示按钮,即可自动复制↓", autoClose: 5, font: P2PFont);
            //End
            //新旧版本区分用的admin
            string url;
            if (Environment.Is64BitOperatingSystem)
            {
                if (admin)
                { url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/main32413.exe"; }
                else { url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew.exe"; }//new  
            }
            else
            {
                url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew32.exe";
            }
            string fileName = Path.Combine(customDirectory, "main.exe");
            string fileMd5;
            if (admin) { fileMd5 = "29d76fc2626c66925621d475f3a6827a"; } else { fileMd5 = "08160296509deac13e7d12c8754de9ef"; }
            ;
            string fileMd532 = "640ffdaa2a7b249d9c301102419a69cb";

            bool needsDownload = false;

            if (File.Exists(fileName))
            {
                string md5Hash = GetFileMD5Hash(fileName);
                if (md5Hash == fileMd5)
                {
                    log("64位核心已存在且安全校验通过");
                }
                else
                {
                    if (md5Hash == fileMd532)
                    {
                        log("32位核心已存在且安全校验通过");
                        needsDownload = false;
                    }
                    else
                    {
                        if (md5Hash == null)
                        {
                            log("出现错误，进程终止");
                            role = "0";  //未开启或是重置状态
                            badge3.Visible = false; badge3.State = TState.Default; badge3.Text = "Null";
                            return;
                        }
                        else
                        {
                            log("核心不存在或安全校验不通过,重新Download中");
                            needsDownload = true;
                        }
                    }
                }
            }
            else
            {
                log("核心不存在或安全校验不通过,重新Download中");
                needsDownload = true;
            }
            //Download
            if (needsDownload)
            {
                progress1.ShowInTaskbar = true;
                progress1.Visible = true;
                progress1.Value = 0;
                // 下载
                log("Downloader启动");
                using (var unityClient = new HttpClient())
                using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                int progress = (int)((downloadedBytes * 100) / totalBytes);
                                progress1.Value = progress;
                            }
                        }
                    }
                }
                progress1.Visible = false;
                progress1.Value = 0;
                progress1.ShowInTaskbar = false;
                log("Task:下载核心中..");
            }
            //清除状态
            progress1.Visible = false;
            progress1.Value = 0;
            progress1.ShowInTaskbar = false;
            log("构造启动参数中..");
            log("复制加入信息中..");
            try
            {
                Clipboard.SetText($"127.0.0.1:{randomPort}");
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Oops,出了点小问题", "复制加入地址时出错\n请在右侧日志中查找到需要复制的提示码\n错误信息" + ex, AntdUI.TType.Error)
                {
                    CloseIcon = true,
                    Font = AlertFont,
                    Draggable = false,
                    CancelText = null,
                    OkText = "好的"
                });
            }
            log($"加入地址127.0.0.1:{randomPort}");
            //如果启用了OLAN兼容,则对user进行base64解码后重新赋值
            bool EnableOLAN = Form1.config.read<bool>("EnableOLAN", false);
            if (EnableOLAN)
            {// 对 user 进行 Base64 解码
                //重新赋值 我也不知道为什么没有就会报错
                log("已启用OneLauncher兼容模式,尝试解码,若 无需解码，请在设置>兼容选项>OneLauncher 关闭");
                user = materialSingleLineTextField2.Text;
                port = materialSingleLineTextField3.Text;
                user = Base64Decode(user);
                string[] parts = user.Split(':');
                user = parts[0];
                string OLANVersion = parts.Length > 1 ? parts[1] : string.Empty;
                log($"解码成功,User{user},OlanVersion{OLANVersion}");
            }
            // 构造启动参数
            //如果开了自选Relay，用Relay方式构建
            bool userelay = Form1.config.read<bool>("EnableRelay", false);
            if (userelay)
            {
                string RelayServer = Form1.config.read<string>("Server", "None");
                log($"已启用Relay,检索到_{RelayServer}");
                //  AntdUI.Message.info(Program.MainForm, $"触发Relay {RelayServer}", autoClose: 5, font: P2PFont);
                if (RelayServer == "None") //无效的Server,使用默认构建再顺便删了配置项
                {
                    arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -token {tokenNormal}";
                    Form1.config.delete("Server");
                }
                arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -relaynode {RelayServer} -token {tokenNormal}";
            }
            else
            {
                arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -token {tokenNormal}";
            }
            //其实UDP与RELAY不兼容,我懒
            if (TCP)
            {
                arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -token {tokenNormal}";
            }
            else
            {
                arguments = $"-node {localName} -appname Minecraft{randomPort} -peernode {user} -dstip 127.0.0.1 -dstport {port} -srcport {randomPort} -protocol udp -token {tokenTest}";
            }//UDP
            //二次校验
            if (!File.Exists(fileName) || !new[] { fileMd5, fileMd532 }.Contains(GetFileMD5Hash(fileName)))
            { log("二次校验失败,进程已终止"); stopp2p(); return; }
            log($"{(GetFileMD5Hash(fileName) == fileMd5 ? "64" : "32")}位核心校验通过，二次校验成功");
            //正常启动
            if (useoldway)
            {
                // 如果勾选了 checkbox1，则显示原版启动窗口
                Process.Start(new ProcessStartInfo()
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true
                });
            }
            else
            {
                Process process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false; // 关闭外壳程序以支持重定向
                process.StartInfo.RedirectStandardOutput = true; // 重定向标准输出
                process.StartInfo.RedirectStandardError = true; // 重定向错误输出
                process.StartInfo.CreateNoWindow = true; // 不显示窗口
                process.OutputDataReceived += (processSender, outputEventArgs) =>
                {
                    if (!string.IsNullOrEmpty(outputEventArgs.Data))
                    {
                        log(outputEventArgs.Data); // 将输出内容记录到 RichTextBox
                    }
                };

                process.ErrorDataReceived += (processSender, errorEventArgs) =>
                {
                    if (!string.IsNullOrEmpty(errorEventArgs.Data))
                    {
                        log(errorEventArgs.Data); // 将错误内容记录到 RichTextBox
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                AntdUI.Message.info(Program.MainForm, "已尝试启动进程,日志内容将输出至右侧哦", autoClose: 5, font: P2PFont);
                log("已尝试启动");
                Joiner.Badge = "运行中";
                CreateFloatButton(new FloatButton.Config(Program.MainForm, new[]{
                new FloatButton.ConfigBtn("CloseButton", Properties.Resources.close){
                Text = "关闭",
                Tooltip = "关闭P2P核心-all",
                Round = true,
                Type = TTypeMini.Primary,
                Radius = 6,
                Enabled = true,
                Loading = false}}, _ => stopp2p())
                {
                    Align = TAlign.BR,
                    Vertical = true,
                    TopMost = false,
                    MarginX = 24,
                    MarginY = 24,
                    Gap = 10,
                    Font = P2PFont
                });
                if (EnableServerPost)
                {
                    // 启动多播发送线程
                    Server_Post.Post_Thread = new Thread(() =>
                    {
                        try
                        {
                            log("房间多播进程启动");
                            Server_Post.Post_Main(int.Parse(randomPort));
                        }
                        catch (Exception ex)
                        {
                            log("多播发送线程异常：" + ex.Message);
                        }
                    });
                    Server_Post.Post_Thread.Start();
                }
                //log(arguments);
#pragma warning disable CS4014
                //Task.Run(() =>
                //{
                //Thread.Sleep(3000);
                //this.BeginInvoke((MethodInvoker)(() =>
                //{
                //materialLabel1.Visible = true;
                //_ping = new TcpPing("127.0.0.1", int.Parse(randomPort), 1000);
                //_ping.OnPingResult += (ip, latency) => UpdateLabel(materialLabel1, ip, latency);
                //_ping.Start();
                //}));
                //});
#pragma warning restore CS4014
            }
        }

        private void materialSingleLineTextField2_Click(object sender, EventArgs e)
        {
            if (materialSingleLineTextField2.Text.Equals("输入提示码"))
            {
                materialSingleLineTextField2.Text = "";
            }
        }

        private void materialSingleLineTextField3_Click(object sender, EventArgs e)
        {
            if (materialSingleLineTextField3.Text.Equals("输入目标端口"))
            {
                materialSingleLineTextField3.Text = "";
            }
        }
        private string user;
        private string port;

        private void materialSingleLineTextField2_LostFocus(object sender, EventArgs e)
        {
            {
                // 获取文本框中的内容并赋值给变量提示码
                string text1 = materialSingleLineTextField2.Text;
                if (string.IsNullOrWhiteSpace(text1) || text1.Equals("输入提示码"))
                {
                    AntdUI.Message.warn(Program.MainForm, "你好像什么都没有写哦(提示码)", autoClose: 5, font: P2PFont);
                    materialSingleLineTextField2.Text = "输入提示码";
                }
                else
                {
                    if (text1.Contains("邀请你"))
                    {
                        AntdUI.Message.error(Program.MainForm, "提示码中可能包含有错误信息！", autoClose: 5, font: P2PFont);
                        AntdUI.Message.error(Program.MainForm, "[提示码]仅包含默认邀请信息后面的部分,怎么全复制上来了?\n例如 DESKTOP-HOMOCO11451\n如果您认为这是正确的,请继续操作,单击此处关闭此提醒", autoClose: 0, font: AlertFont);
                        user = materialSingleLineTextField2.Text;
                        log($"[?]目标提示码为 {user}");
                        log($"[?]变量赋值完成：user= {user}");
                    }
                    else
                    {
                        user = materialSingleLineTextField2.Text;
                        log($"目标提示码为 {user}");
                        log($"变量赋值完成：user= {user}");
                    }
                }
            }
        }

        private void materialSingleLineTextField3_LostFocus(object sender, EventArgs e)
        {
            {
                string text2 = materialSingleLineTextField3.Text;
                if (string.IsNullOrWhiteSpace(text2) || text2.Equals("输入目标端口"))
                {
                    AntdUI.Message.warn(Program.MainForm, "你好像什么都没有写哦(端口)", autoClose: 5, font: P2PFont);
                    materialSingleLineTextField3.Text = "输入目标端口";
                }
                else
                {
                    if (int.TryParse(text2, out int number))
                    {
                        if (number >= 1 && number <= 65535)
                        {
                            port = materialSingleLineTextField3.Text;
                            log($"目标端口为 {port}");
                            log($"变量赋值完成：port= {port}");
                        }
                        else
                        {
                            AntdUI.Message.error(Program.MainForm, "端口超出范围(1~65535)", autoClose: 5, font: P2PFont);
                            log("端口超出范围");
                        }
                    }
                    else
                    {
                        AntdUI.Message.error(Program.MainForm, "端口错误(1~65535)", autoClose: 5, font: P2PFont);
                        log("端口错误,非1-65535的数字");
                    }
                }
            }
        }
        //通用组件
        public static string GetFileMD5Hash(string filePath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    MD5 md5 = MD5.Create();
                    byte[] hashValue = md5.ComputeHash(stream);
                    StringBuilder hex = new StringBuilder(hashValue.Length * 2);
                    foreach (byte b in hashValue)
                    {
                        hex.AppendFormat("{0:x2}", b);
                    }
                    return hex.ToString();
                }
            }
            catch (Exception ex)
            {
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Oops,出了点小问题", "计算MD5时发现问题,可能是杀毒软件给拦了,要不咱重新试试？\n错误信息：" + ex, AntdUI.TType.Warn)
                    {
                        CloseIcon = true,
                        Font = AlertFont,
                        Draggable = false,
                        CancelText = null,
                        OkText = "好的"
                    });
                }
                return null;
                //throw new Exception("Error computing MD5 hash for file " + filePath, ex);
            }
        }
        //LOG区域
        public void log(string message)
        {
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

            string logFilePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "MCZLFAPP", "Temp", "APPLog.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
            File.AppendAllText(logFilePath, logMessage);

            if (richTextBoxLog.InvokeRequired)
            {
                richTextBoxLog.Invoke(new Action(() =>
                {
                    LogToRichTextBox(message);
                }));
            }
            else
            {
                LogToRichTextBox(message);
            }
        }
        private void LogToRichTextBox(string message)
        {
            // 定义字符与方法的映射关系
            Dictionary<string, Action> methodMap = new Dictionary<string, Action>
        {
        //{ "3.21.12", () => log("LogListener已成功被加载") },//Lamba表达式可以跑code
        //{ "connect", Method }//字典只能酱紫
        { "3.21.12", () => log("LogListener已成功被加载") },
        { "NAT type", () => HandleNatType(message) }, // 单独调用 HandleNatType 方法,这里太小写不下
        { "login ok", () => { badge3.State = TState.Warn; badge3.Text = "正在处理中...|与服务器交换信息"; } },
        { "P2PNetwork init start", () => log("正在尝试连接P2PNetwork") },
        { "NAT detect error", () => log("NAT类型探测失败 i/o timeout") },
        { "LISTEN ON PORT", () => {log("Success:成功在本地创建监听端口");badge3.State = TState.Success; badge3.Text = "已连接";} },
        //{ "relay", () => badge3.State = TState.Warn},
        { "sdwan init ok", () => { badge3.State = TState.Warn; badge3.Text = role == "1" ? "正在等待被连接..." : "正在尝试连接..."; } },
        { "connection ok", () => { badge3.State = role == "1" ? TState.Success : TState.Warn; badge3.Text = "已连接"; } },
        { "handShakeC2C ok", () => { badge3.State = role == "1" ? TState.Success : TState.Warn; badge3.Text = "已连接"; } },
        { "i/o timeout", () => {log("i/o超时,可能是端口无法连接,也可能是运营商动手脚了,建议检查一下端口是否有误并继续等待");} },
        { "no such host", () => Program.alerterror("程序未能够连接到HOST,可能是防火墙拦截,或是根本没有授予网络访问权限")},
        { "it will auto reconnect when peer node online", () => Program.alerterror("房间不在线,请检查是否有输入错误,或好友是否正确的启动了房间")},
        { "peer offline", () => {badge3.State = TState.Error;badge3.Text = "对方不在线";Program.alerterror("对方不在线,请检查是否有输入错误,或好友是否正确的启动了房间");  } },
        { "Usage:", () => {stopp2p();badge3.State = TState.Error;badge3.Text = "触发Usage_Bug";Program.alerterror("这是一个Bug,你可以反馈一下发生了什么吗?\n请不要直接对着这个窗口拍照，请上传完整日志");  } },

     };
            // 检查消息中是否包含特定字符
            foreach (var pair in methodMap)
            {
                if (message.Contains(pair.Key))
                {
                    if (pair.Value != null)
                    {
                        pair.Value.Invoke();
                    }
                }
            }
            richTextBoxLog.AppendText(message + Environment.NewLine);
            if (richTextBoxLog.TextLength > 0)
            {
                try
                {
                    richTextBoxLog.ScrollToCaret();
                }
                catch (COMException ex)
                {
                    bool showbug1 = Form1.config.read<bool>("ShowP2PBug", false);
                    if (showbug1)
                    {
                        AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, "Error", "程序进程立正了,发生预料之外的错误\n" + ex.Message, TType.Error));
                    }
                }
            }
        }
        //log结束
        //end
        public void stopp2p()
        {
            role = "0";  //重置状态
            Opener.Badge = null;
            Joiner.Badge = null;
            //移除floatbutton
            ClearAllFloatButtons();
            //--↑移除floatbutton
            materialLabel1.Visible = false;
            //_ping.Stop();
            badge3.Visible = false; badge3.State = TState.Default; badge3.Text = "Null";//清除状态
            infobutton.Visible = false;
            alert1.Visible = false;
            Server_Post.Stop_Post();
            log("Plugins卸载成功");
            Process[] processes = Process.GetProcessesByName("main");
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill(); // 强制结束进程
                    process.WaitForExit(); // 等待进程完全退出
                    log("进程已结束~");
                    AntdUI.Message.info(Program.MainForm, $"已结束进程", autoClose: 5, font: P2PFont);
                    Opener.Badge = null;
                    Joiner.Badge = null;
                }
                catch (Exception ex)
                {
                    AntdUI.Message.error(Program.MainForm, $"无法关闭进程: {ex.Message}", autoClose: 15, font: P2PFont);
                }
            }

            if (processes.Length == 0)
            {
                AntdUI.Message.error(Program.MainForm, "未找到进程", autoClose: 15, font: P2PFont);
            }
        }
        bool admin;
        private void P2PMode_Load(object sender, EventArgs e)
        {
            CloudAlert().ConfigureAwait(false);
            LoadVersionFromParent();
            string gonggao = $"感谢您使用Minecraft Connect Tool\n群聊 690625244       《欢迎加入ヾ(≧▽≦*)o\n仅供Minecraft联机及其他合法用途拓展使用,违法使用作者不负任何责任\n========================================================\n当前版本Minecraft Connect Tool {version}";
            log(gonggao);
            log("温馨提示:如果不点击右下角关闭按钮，核心会继续在后台运行~");
            if (Program.admin == 1)
            { admin = true; }
            else { admin = false; }
            if (Process.GetProcessesByName("main").Length > 0)
            {
                log("Core ==> running");
            }
            bool ifwin7 = IsWindows7OrLower();
            AntdUI.Config.ShowInWindowByMessage = true;
            if (ifwin7)
            {
                log("警告:当前系统版本为Windows7或更低版本,已停止支持，在当前系统版本下可能无法使用Tools某些功能，且可能出现意外崩溃，请勿报告为bug");
                AntdUI.Notification.error(Program.MainForm, "增强提醒*", $"当前系统版本不受支持,建议您升级至Windows10", align: AntdUI.TAlignFrom.BR, autoClose: 0);
            }
            else
            {
                log("systemok");
            }
            bool showbug1 = Form1.config.read<bool>("ShowP2PBug", false);
            if (showbug1)
            {
                materialSingleLineTextField4.Text = "显示所有被捕获的异常，如果遇到无法退出的异常请通过任务栏或任务管理器结束任务";
            }
            badge1.Visible = false;
            if (Program.admin == 2)
            { log("Warn:当前非管理员模式,将运行旧版无需管理员权限核心"); }
            bool usecustomport = Form1.config.read<bool>("usecustomport", false);
            if (usecustomport)
            {
                badge1.Visible = true;
                string customport = Form1.config.read<string>("customport", "None");
                if (!int.TryParse(customport, out int port) || port < 1 || port > 65535)
                {
                    badge1.Text = "错误的自定义端口";
                    badge1.State = TState.Error;
                }
                else
                {
                    badge1.Visible = true;
                }
            }
            if (EnableServerPost)
            {
                badge2.Visible = true;
            }
            bool EnableOLAN = Form1.config.read<bool>("EnableOLAN", false);
            if (EnableOLAN) { badge4.Visible = true; }
            badge3.Visible = false; badge3.State = TState.Default; badge3.Text = "Null";
            bool EnableTL = Form1.config.read<bool>("EnableTL", false);
            if (EnableTL) { TopText.Text = "您正在使用P2P模式进行联机ヾ(≧▽≦*)o(TL)"; materialSingleLineTextField3.Text = "7777"; }
            bool EnableDST = Form1.config.read<bool>("EnableDST", false);
            if (EnableDST) { TopText.Text = "您正在使用P2P模式进行联机ヾ(≧▽≦*)o(DST)"; DSTFill(); }
            bool EnableRelay = Form1.config.read<bool>("EnableRelay", false);
            if (EnableRelay) { badge5.Visible = true; }
            bool TCP = Form1.config.read<bool>("TCP", true);
            if (!TCP) { TopText.Text = "您正在使用P2P模式进行联机ヾ(≧▽≦*)o(UDP)"; }
            bool Testchannel = Form1.config.read<bool>("Testchannel", false);
            if (Testchannel) { TopText.Text = "您正在使用P2P模式进行联机ヾ(≧▽≦*)o(测试)";
            tokenNormal = "7196174974940052261";//Token重复值
            }
        }

        private void richTextBoxLog_TextChanged_1(object sender, EventArgs e)
        {

        }
        private static readonly string CacheFileName = Path.Combine(Path.GetTempPath(),"MCZLFAPP","Temp","MCZLFAPP_Temp_CloudAlertCache.flag");
        //private const string CountFileName = "MCZLFAPP_Temp_CloudAlertCount.txt";
        //private int _requestCount = 0;

        private async Task CloudAlert()
        {
            string url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/cloudalert";
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFileName));
            #region newfetch
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string cloudalert = await client.GetStringAsync(url);
                    log("📢" + cloudalert);
                    if (cloudalert.IndexOf("[info]", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string lastInfo = File.Exists(CacheFileName) ? File.ReadAllText(CacheFileName) : "";
                        if (lastInfo != cloudalert)          // 内容变化才弹
                        {
                            File.WriteAllText(CacheFileName, cloudalert);
                            AntdUI.Message.info(Program.MainForm, cloudalert.Replace("[info]", "").Trim(), autoClose: 5, font: P2PFont);
                        }
                    }

                    //OTHER TYPE
                    if (cloudalert.IndexOf("[warn]", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        AntdUI.Message.warn(Program.MainForm, cloudalert.Replace("[warn]", "").Trim(), autoClose: 0, font: P2PFont);
                    }
                    if (cloudalert.IndexOf("[error]", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        AntdUI.Message.error(Program.MainForm, cloudalert.Replace("[error]", "").Trim(), autoClose: 0, font: P2PFont);
                    }
                    if (cloudalert.IndexOf("[alerterror]", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Program.alerterror(cloudalert.Replace("[error]", "").Trim());
                    }
                    if (cloudalert.IndexOf("[alertwarn]", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Program.alertwarn(cloudalert.Replace("[alertwarn]", "").Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(Program.MainForm, $"获取云公告失败,原因:" + ex.Message, autoClose: 5, font: P2PFont);
                log("获取云公告内容失败：" + ex.Message);
            }
            #endregion

#if Old_CloudAlertFetch
            string tempPath = Path.Combine(Path.GetTempPath(), "MCZLFAPP"); // 获取系统临时文件夹下的MCZLFAPP目录路径
            string cacheFilePath = Path.Combine(tempPath, CacheFileName); // 缓存文件路径
            string countFilePath = Path.Combine(tempPath, CountFileName); // 请求次数文件路径
            try
            {
                // 从文件中读取请求次数
                if (File.Exists(countFilePath))
                {
                    using (StreamReader reader = new StreamReader(countFilePath))
                    {
                        _requestCount = int.Parse(await reader.ReadToEndAsync());
                    }
                }
                if (_requestCount % 10 == 0)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string cloudalert = await client.GetStringAsync(url);
                        using (StreamWriter writer = new StreamWriter(cacheFilePath, false))
                        {
                            await writer.WriteAsync(cloudalert);
                        }
                        log("📢" + cloudalert);
                    }
                }
                else
                {
                    if (File.Exists(cacheFilePath))
                    {
                        using (StreamReader reader = new StreamReader(cacheFilePath))
                        {
                            string cloudalert = await reader.ReadToEndAsync();
                            log("✨" + cloudalert);
                        }
                    }
                    else
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            string cloudalert = await client.GetStringAsync(url);
                            using (StreamWriter writer = new StreamWriter(cacheFilePath, false))
                            {
                                await writer.WriteAsync(cloudalert);
                            }
                            log("📢" + cloudalert);
                        }
                    }
                }
                _requestCount++;
                using (StreamWriter writer = new StreamWriter(countFilePath, false))
                {
                    await writer.WriteAsync(_requestCount.ToString());
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(Program.MainForm, $"获取云公告失败,原因:" + ex.Message, autoClose: 5, font: P2PFont);
                log("获取云公告内容失败：" + ex.Message);
            }
#endif
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button4.Visible = true;
            //GetNode
            string node = System.Environment.MachineName;
            //GetNetVersion
            string Netversion = System.Environment.Version.ToString();
            //GetSysVersion
            string SystemEvVersion = System.Environment.OSVersion.VersionString + " " + (System.Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
            //AntdUI.Message.info(Program.MainForm, $"调试信息：当前版本{version}", autoClose: 5, font: P2PFont);
            try { Clipboard.SetText($"协助调试信息\n当前版本{version}\nNode:{node}\nEnvironment版本{Netversion}\nSystem {SystemEvVersion}"); } catch { }
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"请将此提示窗口[截图]发送,或直接粘贴剪切板里的", "\n当前版本" + version + "\nNode:" + node + "\nEnviroment版本" + Netversion + "\nSystem " + SystemEvVersion, AntdUI.TType.Warn)
            {
                CloseIcon = true,
                Font = P2PFont,
                Draggable = false,
                CancelText = null,
                OkText = "好了"
            });

        }
        private void LoadVersionFromParent()
        {
            version = Form1.version;
            //var parent = this.Parent; // 从当前 UserControl 的 Parent 开始
            //while (parent != null)
            //{
            //    var form1 = parent as Form1;
            //    if (form1 != null)
            //    {
            //        version = form1.PageHeader.SubText;
            //        log(version);
            //        return;
            //    }
            //    parent = parent.Parent; // 继续向上查找
            //}
            log(version);
            //version = "Unknown";
            Debug.Print("Failed to get version");
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, "确认操作", "使用旧版启动方式可能包含诸多无法预知的问题，您确定要继续吗", TType.Warn)
                {
                    Font = new Font("Microsoft YaHei UI", 9f),
                    OkText = "确认",
                    CancelText = null,
                    OnOk = config =>
                    {
                        checkBox2.Checked = true;
                        useoldway = true;
                        return true;
                    },
                    Btns = new AntdUI.Modal.Btn[]
                    {
                new AntdUI.Modal.Btn("cancel", "取消", AntdUI.TTypeMini.Default)
                    },
                    OnBtns = btn =>
                    {
                        if (btn.Name == "cancel")
                        {
                            checkBox2.Checked = false;
                            useoldway = false;
                            return true;
                        }
                        return false;
                    }
                });
            }
            else
            {
                useoldway = false; // 如果取消勾选，确保字段被还原
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void materialSingleLineTextField4_Click(object sender, EventArgs e)
        {

        }

        private async void button4_Click(object sender, EventArgs e)
        {
            string url;
            string tempDirectory = Path.GetTempPath();
            string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
            Directory.CreateDirectory(customDirectory);
            Directory.SetCurrentDirectory(customDirectory);
            if (Environment.Is64BitOperatingSystem)
            {
                if (admin)
                { url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/main32413.exe"; }
                else { url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew.exe"; }//new  
            }
            else
            {
                url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew32.exe";
            }
            string fileName = Path.Combine(customDirectory, "main.exe");
            string fileMd5;
            if (admin) { fileMd5 = "29d76fc2626c66925621d475f3a6827a"; } else { fileMd5 = "08160296509deac13e7d12c8754de9ef"; }
            ;
            string fileMd532 = "640ffdaa2a7b249d9c301102419a69cb";
            bool needsDownload = false;
            if (File.Exists(fileName))
            {
                string md5Hash = GetFileMD5Hash(fileName);
                if (md5Hash == fileMd5)
                {
                    log("64位核心已存在且安全校验通过");
                }
                else
                {
                    if (md5Hash == fileMd532)
                    {
                        log("32位核心已存在且安全校验通过");
                        needsDownload = false;
                    }
                    else
                    {
                        if (md5Hash == null)
                        {
                            log("出现错误，进程终止");
                            role = "0";  //未开启或是重置状态
                            badge3.Visible = false; badge3.State = TState.Default; badge3.Text = "Null";
                            return;
                        }
                        else
                        {
                            log("核心不存在或安全校验不通过,重新Download中");
                            needsDownload = true;
                        }
                    }

                }
            }
            else
            {
                log("核心不存在或安全校验不通过,重新Download中");
                needsDownload = true;
            }
            //Download
            if (needsDownload)
            {
                progress1.ShowInTaskbar = true;
                progress1.Visible = true;
                progress1.Value = 0;
                // 下载
                log("Downloader启动");
                using (var unityClient = new HttpClient())
                using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                int progress = (int)((downloadedBytes * 100) / totalBytes);
                                progress1.Value = progress;
                            }
                        }
                    }
                }
                progress1.Visible = false;
                progress1.Value = 0;
                progress1.ShowInTaskbar = false;
                log("Success");
            }
            //清除状态
            progress1.Visible = false;
            progress1.Value = 0;
            progress1.ShowInTaskbar = false;
        }
        private bool IsWindows7OrLower()
        {
            OperatingSystem os = Environment.OSVersion;
            Version osVersion = os.Version;
            return os.Platform == PlatformID.Win32NT && osVersion.Major < 6 || (osVersion.Major == 6 && osVersion.Minor <= 1);
            //private bool IsWindows7OrLower()
            //{
            //    OperatingSystem os = Environment.OSVersion;
            //    Version osVersion = os.Version;
            //    if (os.Platform == PlatformID.Win32NT && osVersion.Major <= 6 && osVersion.Minor <= 1)
            //    {
            //        return true;
            //    }
            //    return false;
            //}
        }
        public void AlreadyCore()
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Wow,这样真的不会出问题吗", "你貌似启动了多个P2P核心,可能会报错导致无法连接哦\n点击右下角的关闭按钮即可关闭所有已启动的核心\n如果您知道您在做什么，点击确定忽略该信息", AntdUI.TType.Warn)
            {
                CloseIcon = true,
                Font = AlertFont,
                Draggable = false,
                CancelText = null,
                OkText = "确定"
            });
        }

        private void infobutton_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(infobutton.Text);
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Oops,出了点小问题", "复制信息时出现错误\n请手动输入或在右侧日志中查找到需要复制的内容\n错误信息" + ex, AntdUI.TType.Error)
                {
                    CloseIcon = true,
                    Font = AlertFont,
                    Draggable = false,
                    CancelText = null,
                    OkText = "好的"
                });
            }
        }

        private void alert1_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(infobutton.Text);
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Oops,出了点小问题", "复制信息时出现错误\n请手动输入或在右侧日志中查找到需要复制的内容\n错误信息" + ex, AntdUI.TType.Error)
                {
                    CloseIcon = true,
                    Font = AlertFont,
                    Draggable = false,
                    CancelText = null,
                    OkText = "好的"
                });
            }
        }
        private void HandleNatType(string message)
        {
            //            MessageBox.Show("触发", "NAT类型提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            string pattern = @"NAT type\s*:\s*(\w+)";
            Match match = Regex.Match(message, pattern);

            if (match.Success)
            {
                string natType = match.Groups[1].Value;

                if (natType == "2")
                {
                    //MessageBox.Show("当前NAT类型为对称形 Symmetric NAT,可能连接时间要非常久...或者可能连不上", "增强提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (hasreadofflinemessage)
                    { log("当前NAT类型为对称形 Symmetric NAT,可能连接时间要非常久...或者可能连不上"); }
                    else
                    {
                        hasreadofflinemessage = true;
                        Program.alertwarn("当前NAT类型为对称形 Symmetric NAT,可能连接时间要非常久...\n或者可能连不上");
                    }
                }
                else
                {
                    log($"NatType:{natType},Support");
                }
            }
            else
            {
                log("NatType:?");
            }
        }
        // 编码
        public static string Base64Decode(string base64EncodedData)
        {
            try
            {
                // 将Base64编码的字符串转换为字节数组
                byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);

                // 将字节数组解码为UTF-8格式的字符串
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (Exception ex)
            {
                // 弹出异常信息
                MessageBox.Show($"解码失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void divider2_Click(object sender, EventArgs e)
        {

        }
        // 创建 FloatButton 并记录引用
        private void CreateFloatButton(FloatButton.Config cfg)
        {
            var btn = FloatButton.open(cfg);   // object 类型
            _floatButtons.Add(btn);
        }
        private void ClearAllFloatButtons()
        {
            foreach (var btn in _floatButtons)
            {
                try
                {
                    // 反射调用 Close()
                    btn.GetType().GetMethod("Close")?.Invoke(btn, null);
                }
                catch { /* ignore */ }
            }
            _floatButtons.Clear();
        }
        private void UpdateLabel(MaterialSkin.Controls.MaterialLabel label, string ip, int latency)
        {
            if (label.InvokeRequired)
            {
                label.Invoke(new Action(() => UpdateLabel(label, ip, latency)));
            }
            else
            {
                if (latency >= 0)
                {
                    label.Text = $"Ping:{latency}ms";
                }
                else
                {
                    label.Text = $"Ping:Failed";
                }
            }
        }
        public void DSTFill()
        {
            AntdUI.Message.info(Program.MainForm, "当前正处于饥荒联机版模式\n如需关闭请前往 设置>兼容选项 关闭", autoClose: 5, font: Program.AlertFont);
            materialSingleLineTextField3.Visible = false;
            materialSingleLineTextField2.Location = new System.Drawing.Point(34, 279);
        }
        public async void DSTJoin()
        {
            //定位符
            role = "2";
            port = "0";
            user = materialSingleLineTextField2.Text;
            if (string.IsNullOrEmpty(user) || user.Trim().Equals("输入提示码", StringComparison.OrdinalIgnoreCase))
            {AntdUI.Message.warn(Program.MainForm, "你好像没有填写或确认[提示码]哦", autoClose: 5, font: P2PFont); log("user无赋值内容"); return;}
            //获取默认Node
            badge3.Visible = true; badge3.State = TState.Processing; badge3.Text = "正在处理中...";
            string localName = Environment.MachineName;
            Random random = new Random();
            string randomPort = random.Next(1, 65536).ToString(); // 生成随机端口号并转换为字符串
            string tempDirectory = Path.GetTempPath();
            string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
            Directory.CreateDirectory(customDirectory);
            Directory.SetCurrentDirectory(customDirectory);
            //清理运行垃圾
            if (File.Exists("config.json")) File.Delete("config.json");
            if (File.Exists("config.json0")) File.Delete("config.json0");
            //输出到常驻信息
            alert1.Text = "加入命令→";
            infobutton.Text = $"c_connect('127.0.0.1', 10999)";
            alert1.Visible = true;
            infobutton.Visible = true;
            AntdUI.Message.info(Program.MainForm, $"单击屏幕增强提醒右侧的信息展示按钮,即可自动复制↓", autoClose: 5, font: P2PFont);
            //End
            //新旧版本区分用的admin
            string url;
            if (Environment.Is64BitOperatingSystem)
            {
                if (admin)
                { url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/main32413.exe"; }
                else { url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew.exe"; }//new  
            }
            else
            {
                url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew32.exe";
            }
            string fileName = Path.Combine(customDirectory, "main.exe");
            string fileMd5;
            if (admin) { fileMd5 = "29d76fc2626c66925621d475f3a6827a"; } else { fileMd5 = "08160296509deac13e7d12c8754de9ef"; }
            ;
            string fileMd532 = "640ffdaa2a7b249d9c301102419a69cb";

            bool needsDownload = false;

            if (File.Exists(fileName))
            {
                string md5Hash = GetFileMD5Hash(fileName);
                if (md5Hash == fileMd5)
                {
                    log("64位核心已存在且安全校验通过");
                }
                else
                {
                    if (md5Hash == fileMd532)
                    {
                        log("32位核心已存在且安全校验通过");
                        needsDownload = false;
                    }
                    else
                    {
                        if (md5Hash == null)
                        {
                            log("出现错误，进程终止");
                            role = "0";  //未开启或是重置状态
                            badge3.Visible = false; badge3.State = TState.Default; badge3.Text = "Null";
                            return;
                        }
                        else
                        {
                            log("核心不存在或安全校验不通过,重新Download中");
                            needsDownload = true;
                        }
                    }
                }
            }
            else
            {
                log("核心不存在或安全校验不通过,重新Download中");
                needsDownload = true;
            }
            //Download
            if (needsDownload)
            {
                progress1.ShowInTaskbar = true;
                progress1.Visible = true;
                progress1.Value = 0;
                // 下载
                log("Downloader启动");
                using (var unityClient = new HttpClient())
                using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                int progress = (int)((downloadedBytes * 100) / totalBytes);
                                progress1.Value = progress;
                            }
                        }
                    }
                }
                progress1.Visible = false;
                progress1.Value = 0;
                progress1.ShowInTaskbar = false;
                log("Task:下载核心中..");
            }
            //清除状态
            progress1.Visible = false;
            progress1.Value = 0;
            progress1.ShowInTaskbar = false;
            log("构造启动参数中..");
            //二次校验
            if (!File.Exists(fileName) || !new[] { fileMd5, fileMd532 }.Contains(GetFileMD5Hash(fileName)))
            { log("二次校验失败,进程已终止"); stopp2p(); return; }
            log($"{(GetFileMD5Hash(fileName) == fileMd5 ? "64" : "32")}位核心校验通过，二次校验成功");
            arguments = $"-node DST{localName} -token {tokenTest}";

            //写入并启动

            string dir = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
            string cfg = Path.Combine(dir, "config.json");

            Directory.CreateDirectory(dir); // 确保目录存在

            // 原始模板，一字不改
            // 1. 模板只改两个字面量，其余不动
            const string TEMPLATE = @"{
  ""network"": {
    ""Token"": {tokenTest},
    ""Node"": ""__NODE_PLACEHOLDER__"",
    ""User"": ""linfon182"",
    ""ShareBandwidth"": 10,
    ""ServerHost"": ""api.openp2p.cn"",
    ""ServerPort"": 27183,
    ""PublicIPPort"": 60488
  },
  ""apps"": [
    {
      ""AppName"": ""10998PROXY"",
      ""Protocol"": ""udp"",
      ""UnderlayProtocol"": """",
      ""PunchPriority"": 0,
      ""Whitelist"": """",
      ""SrcPort"": 10998,
      ""PeerNode"": ""__USER_PLACEHOLDER__"",
      ""DstPort"": 10998,
      ""DstHost"": ""localhost"",
      ""PeerUser"": """",
      ""RelayNode"": """",
      ""ForceRelay"": 0,
      ""Enabled"": 1
    },
    {
      ""AppName"": ""10999PROXY"",
      ""Protocol"": ""udp"",
      ""UnderlayProtocol"": """",
      ""PunchPriority"": 0,
      ""Whitelist"": """",
      ""SrcPort"": 10999,
      ""PeerNode"": ""__USER_PLACEHOLDER__"",
      ""DstPort"": 10999,
      ""DstHost"": ""localhost"",
      ""PeerUser"": """",
      ""RelayNode"": """",
      ""ForceRelay"": 0,
      ""Enabled"": 1
    }
  ],
  ""LogLevel"": 0,
  ""MaxLogSize"": 1048576,
  ""TLSInsecureSkipVerify"": false
}";

            // 2. 一次性替换
            string json = TEMPLATE
                    .Replace("__NODE_PLACEHOLDER__", $"DST{localName}")
                    .Replace("__USER_PLACEHOLDER__", user);

            // 3. 强制覆盖，无 BOM
            File.WriteAllText(cfg, json, new UTF8Encoding(false));
            log(json);
            Process process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false; // 关闭外壳程序以支持重定向
            process.StartInfo.RedirectStandardOutput = true; // 重定向标准输出
            process.StartInfo.RedirectStandardError = true; // 重定向错误输出
            process.StartInfo.CreateNoWindow = true; // 不显示窗口
            process.OutputDataReceived += (processSender, outputEventArgs) =>
            {
                if (!string.IsNullOrEmpty(outputEventArgs.Data))
                {
                    log(outputEventArgs.Data); // 将输出内容记录到 RichTextBox
                }
            };

            process.ErrorDataReceived += (processSender, errorEventArgs) =>
            {
                if (!string.IsNullOrEmpty(errorEventArgs.Data))
                {
                    log(errorEventArgs.Data); // 将错误内容记录到 RichTextBox
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            AntdUI.Message.info(Program.MainForm, "已尝试启动进程,日志内容将输出至右侧哦", autoClose: 5, font: P2PFont);
            log("已尝试启动");
            Joiner.Badge = "运行中";
            CreateFloatButton(new FloatButton.Config(Program.MainForm, new[]{
                new FloatButton.ConfigBtn("CloseButton", Properties.Resources.close){
                Text = "关闭",
                Tooltip = "关闭P2P核心-all",
                Round = true,
                Type = TTypeMini.Primary,
                Radius = 6,
                Enabled = true,
                Loading = false}}, _ => stopp2p())
            {
                Align = TAlign.BR,
                Vertical = true,
                TopMost = false,
                MarginX = 24,
                MarginY = 24,
                Gap = 10,
                Font = P2PFont
            });
        }

        private void TopText_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}

