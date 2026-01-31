using AntdUI;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class LinkMode : UserControl
    {
        public LinkMode()
        {
            InitializeComponent();
            materialSingleLineTextField2.LostFocus += new EventHandler(materialSingleLineTextField2_LostFocus);

        }
        public string role = "0"; //0=未开启  1=房主 2=加入方
        private Process _linkProcess;
        private readonly List<object> _floatButtons = new List<object>();


        private async void Opener_Click(object sender, EventArgs e)
        {
            role = "1"; //房主
            if (Process.GetProcessesByName("main").Length > 0) AlreadyCore();
            string tempDirectory = Path.GetTempPath();
            string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
            log("检查LinkMode核心中..");
            string url = "http://mczlf.loft.games/API/link.exe"; //OnlySupport AMD64
            string fileName = Path.Combine(customDirectory, "link.exe");
            string fileMd5 = "559a28f9d51dcbec970d2dbc7f2fd8aa";
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
                    if (md5Hash == null)
                    {
                        log("出现错误，进程终止");
                        role = "0";  // 未开启或是重置状态
                        badge3.Visible = false;
                        badge3.State = TState.Default;
                        badge3.Text = "Null";
                        return;
                    }
                    else
                    {
                        log("核心不存在或安全校验不通过,重新Download中");
                        needsDownload = true;
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
            string arguments;
            var customModal = new InteractiveModal();
            string port = customModal.ShowModal(Program.MainForm, "请输入游戏内提示端口(例如25565)");
            if (string.IsNullOrWhiteSpace(port))
            {
                log("用户取消输入端口，启动终止");
                role = "0";
                badge3.Visible = false;
                badge3.State = TState.Default;
                badge3.Text = "Null";
                return;
            }

            log($"选定端口：{port}，准备启动核心…");
            fileName = Path.Combine(customDirectory, "link.exe");
            arguments = $"-s {port}";

            try
            {
                Process process = new Process();
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;


                // JSON 日志解析
                void OnOutputLine(string line)
                {
                    if (string.IsNullOrWhiteSpace(line)) return;
                    try
                    {
                        dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(line);
                        if (obj != null && obj.msg != null)
                            log(obj.msg.ToString());
                        else
                            log(line);
                    }
                    catch { log(line); }
                }

                process.OutputDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) OnOutputLine(args.Data);
                };
                process.ErrorDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) OnOutputLine(args.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 存到字段，供后续发指令
                _linkProcess = process;

                AntdUI.Message.info(Program.MainForm,
                                    "Link 核心已启动，日志将输出到右侧",
                                    autoClose: 5,
                                    font: Program.AlertFont);
                log("link.exe 启动成功~");
                badge3.Visible = true;
                badge3.State = TState.Success;
                badge3.Text = "Online";
            }
            catch (Exception ex)
            {
                log($"启动失败：{ex.Message}");
                role = "0";
                badge3.Visible = false;
                badge3.State = TState.Default;
                badge3.Text = "Null";
            }

            CreateFloatButton(new FloatButton.Config(Program.MainForm, new[]{
                new FloatButton.ConfigBtn("CloseButton", Properties.Resources.close){
                Text = "关闭",
                Tooltip = "关闭Link核心-all",
                Round = true,
                Type = TTypeMini.Primary,
                Radius = 6,
                Enabled = true,
                Loading = false}}, _ => stoplink())
            {
                Align = TAlign.BR,
                Vertical = true,
                TopMost = false,
                MarginX = 24,
                MarginY = 24,
                Gap = 10,
                Font = Program.AlertFont
            });
        }

        private void LinkMode_Load(object sender, EventArgs e)
        {
            string gonggao = $"感谢您使用Minecraft Connect Tool\n群聊 690625244       《欢迎加入ヾ(≧▽≦*)o\n仅供Minecraft联机及其他合法用途拓展使用,违法使用作者不负任何责任\n========================================================\nLinkMode功能目前可能尚不完善,若有Bug可以反馈";
            log(gonggao);
        }

        private async void Joiner_Click(object sender, EventArgs e)
        {
            string user = materialSingleLineTextField2.Text;
            role = "2"; //加入
            if (Process.GetProcessesByName("main").Length > 0) AlreadyCore();
            string tempDirectory = Path.GetTempPath();
            string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
            //Random random = new Random();
            //string randomPort = random.Next(1, 65536).ToString(); // 生成随机端口号并转换为字符串
            //bool usecustomport = Form1.config.read<bool>("usecustomport", false);
            //if (usecustomport)
            //{
            //    string customport = Form1.config.read<string>("customport", "None");
            //    if (!int.TryParse(customport, out int parsedPort) || parsedPort < 1 || parsedPort > 65535)
            //    {
            //        AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Wow...意料之外呢", "线程小伙读取到了自定义端口信息,但是端口居然是神秘的字符\n读取到的自定义端口：" + customport, AntdUI.TType.Warn)
            //        {
            //            CloseIcon = true,
            //            Font = Program.AlertFont,
            //            Draggable = false,
            //            CancelText = null,
            //            OkText = "好的"
            //        });
            //        return;
            //    }
            //    else
            //    {
            //        randomPort = customport; // 将 customport 的字符串值赋值给 randomPort
            //    }
            //}
            log("检查LinkMode核心中..");
            string url = "http://mczlf.loft.games/API/link.exe"; //OnlySupport AMD64
            string fileName = Path.Combine(customDirectory, "link.exe");
            string fileMd5 = "559a28f9d51dcbec970d2dbc7f2fd8aa";
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
                    if (md5Hash == null)
                    {
                        log("出现错误，进程终止");
                        role = "0";  // 未开启或是重置状态
                        badge3.Visible = false;
                        badge3.State = TState.Default;
                        badge3.Text = "Null";
                        return;
                    }
                    else
                    {
                        log("核心不存在或安全校验不通过,重新Download中");
                        needsDownload = true;
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
            string arguments;
            fileName = Path.Combine(customDirectory, "link.exe");
            arguments = $"-c {user}";
            ////输出到常驻信息
            alert1.Text = "请在局域网世界中查看";
            //infobutton.Text = $"请在局域网世界中查看";
            //alert1.Visible = true;
            //infobutton.Visible = true;
            //AntdUI.Message.info(Program.MainForm, $"单击屏幕增强提醒右侧的信息展示按钮,即可自动复制↓", autoClose: 5, font: Program.AlertFont);
            try
            {
                Process process = new Process();
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;


                // JSON 日志解析
                void OnOutputLine(string line)
                {
                    if (string.IsNullOrWhiteSpace(line)) return;
                    try
                    {
                        dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(line);
                        if (obj != null && obj.msg != null)
                            log(obj.msg.ToString());
                        else
                            log(line);
                    }
                    catch { log(line); }
                }

                process.OutputDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) OnOutputLine(args.Data);
                };
                process.ErrorDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) OnOutputLine(args.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 存到字段，供后续发指令
                _linkProcess = process;

                AntdUI.Message.info(Program.MainForm,
                                    "Link 核心已启动，日志将输出到右侧",
                                    autoClose: 5,
                                    font: Program.AlertFont);
                log("link.exe 启动成功~");

                // 可选：把状态灯改成绿色
                badge3.Visible = true;
                badge3.State = TState.Success;
                badge3.Text = "Online";
            }
            catch (Exception ex)
            {
                log($"启动失败：{ex.Message}");
                role = "0";
                badge3.Visible = false;
                badge3.State = TState.Default;
                badge3.Text = "Null";
            }

            CreateFloatButton(new FloatButton.Config(Program.MainForm, new[]{
                new FloatButton.ConfigBtn("CloseButton", Properties.Resources.close){
                Text = "关闭",
                Tooltip = "关闭Link核心-all",
                Round = true,
                Type = TTypeMini.Primary,
                Radius = 6,
                Enabled = true,
                Loading = false}}, _ => stoplink())
            {
                Align = TAlign.BR,
                Vertical = true,
                TopMost = false,
                MarginX = 24,
                MarginY = 24,
                Gap = 10,
                Font = Program.AlertFont
            });
            //启动轮询
            StartStatusPolling();
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
                        Font = Program.AlertFont,
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
            if ("中转模式|P2P模式".Contains(message)) return;
            var replacements = new Dictionary<string, string>
    {

        { "连接服务器成功，分享联机码后联机！", "连接服务器成功，将提示码分享给好友即可快速开始联机" },
        { "分享码", "提示码" }, //适应MCT本土化叫法
        { "联机码", "提示码" },
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
        { "提示码", () => { alert1.Visible = true; infobutton.Visible = true;infobutton.Text=ExtractPromptCode(message); MessageBox.Show($"您的提示码为{message}\n已复制入剪切板中,快去粘贴给小伙伴吧", "增强提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);AntdUI.Notification.info(Program.MainForm, "增强提醒", $"您的提示码为{message}\n已复制入剪切板中,快去粘贴给小伙伴吧", align: AntdUI.TAlignFrom.BR, font: Program.AlertFont);try{Clipboard.SetText($"邀请你加入我的Minecraft联机房间！\n提示码为 {message}\n复制时请勿带上前面的中文哦");}catch{ } } },
        { "未通过MC流量检测，程序终止", () => { badge3.State = TState.Error; badge3.Text = "请先启动游戏后再开启房间"; Program.alerterror("请先启动游戏后再开启房间");stoplink(); } },
        { "解析响应失败", () => {log("与服务器的连接受阻，可能是服务器被攻击造成离线");stoplink(); } },
        { "NAT detect error", () => log("NAT类型探测失败 i/o timeout") },
        { "LISTEN ON PORT", () => {log("Success:成功在本地创建监听端口");badge3.State = TState.Success; badge3.Text = "已连接";} },
        //{ "relay", () => badge3.State = TState.Warn},
        { "sdwan init ok", () => { badge3.State = TState.Warn; badge3.Text = role == "1" ? "正在等待被连接..." : "正在尝试连接..."; } },
        { "connection ok", () => { badge3.State = role == "1" ? TState.Success : TState.Warn; badge3.Text = "已连接"; } },
        { "handShakeC2C ok", () => { badge3.State = role == "1" ? TState.Success : TState.Warn; badge3.Text = "已连接"; } },
        { "i/o timeout", () => {log("i/o超时,可能是端口无法连接,也可能是运营商动手脚了,建议检查一下端口是否有误并继续等待");} },
        { "no such host", () => Program.alerterror("程序未能够连接到HOST,可能是防火墙拦截,或是根本没有授予网络访问权限")},
        { "it will auto reconnect when peer node online", () => Program.alerterror("房间不在线,请检查是否有输入错误,或好友是否正确的启动了房间")},
        { "房间不存在，联机失败", () => {badge3.State = TState.Error;badge3.Text = "对方不在线";Program.alerterror("对方不在线,请检查是否有输入错误,或好友是否正确的启动了房间");  } },
        { "Usage:", () => {stoplink();badge3.State = TState.Error;badge3.Text = "触发Usage_Bug";Program.alerterror("这是一个Bug,你可以反馈一下发生了什么吗?\n请不要直接对着这个窗口拍照，请上传完整日志");  } },

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
            if (message == "连接服务器成功，分享联机码后联机！")
                    {
                        message = "连接服务器成功，将提示码分享给好友即可快速开始联机";
                        Debug.WriteLine($"[TextReplaced] {message}"); //让Ai帮我改改排版结果给我上没用的注释
                    }//写在前面会重新触发上岛和增强提醒，所以写在这里了T_T
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
        public void stoplink()
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
            Process[] processes = Process.GetProcessesByName("link");
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill(); // 强制结束进程
                    process.WaitForExit(); // 等待进程完全退出
                    log("进程已结束~");
                    AntdUI.Message.info(Program.MainForm, $"已结束进程", autoClose: 5, font: Program.AlertFont);
                    Opener.Badge = null;
                    Joiner.Badge = null;
                }
                catch (Exception ex)
                {
                    AntdUI.Message.error(Program.MainForm, $"无法关闭进程: {ex.Message}", autoClose: 15, font: Program.AlertFont);
                }
            }

            if (processes.Length == 0)
            {
                AntdUI.Message.error(Program.MainForm, "未找到进程", autoClose: 15, font: Program.AlertFont);
            }
            label1.Visible = false;
            label1.Text = null;//清空文本
            StopStatusPolling();//结束轮询
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
        private void CreateFloatButton(FloatButton.Config cfg)
        {
            var btn = FloatButton.open(cfg);   // object 类型
            _floatButtons.Add(btn);
        }


        public void AlreadyCore()
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Wow,这样真的不会出问题吗", "你貌似启动了多个Link核心,可能会报错导致无法连接哦\n点击右下角的关闭按钮即可关闭所有已启动的核心\n如果您知道您在做什么，点击确定忽略该信息", AntdUI.TType.Warn)
            {
                CloseIcon = true,
                Font = Program.AlertFont,
                Draggable = false,
                CancelText = null,
                OkText = "确定"
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string command = materialSingleLineTextField1.Text;
            _linkProcess.StandardInput.WriteLine(command);
        }
        private string ExtractPromptCode(string fullText)
        {
            if (string.IsNullOrWhiteSpace(fullText))
                return string.Empty;

            // 同时支持中文和英文冒号，取最后一个冒号后的内容
            int lastColonIndex = fullText.LastIndexOfAny(new[] { '：', ':' });
            if (lastColonIndex == -1)
                return string.Empty;

            return fullText.Substring(lastColonIndex + 1).Trim();
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
                    Font = Program.AlertFont,
                    Draggable = false,
                    CancelText = null,
                    OkText = "好的"
                });
            }
        }
        // 轮询定时器
        private System.Timers.Timer _statusTimer;

        // 最新一次 status 命令的反馈内容（仅 success/msg）
        private string statusCommandFeedback = string.Empty;
        #region 状态轮询

/// <summary>
/// 启动成功后调用：开始每 1 s 查询一次连接状态
/// </summary>
        private void StartStatusPolling()
        {
            // 如果之前已经开过，先停掉
            _statusTimer?.Stop();
            _statusTimer?.Dispose();

            _statusTimer = new System.Timers.Timer(1000) { AutoReset = true, SynchronizingObject = this };
            _statusTimer.Elapsed += (_, __) => SendStatusCommand();
            _statusTimer.Start();
        }

        /// <summary>
        /// 发送 status 命令并等待回包
        /// </summary>
        private void SendStatusCommand()
        {
            if (_linkProcess == null || _linkProcess.HasExited) return;

            // 用 GUID 当 id，避免重复
            string id = Guid.NewGuid().ToString("N");
            string cmdJson = $"{{\"id\":\"{id}\",\"order\":\"status\"}}";

            // 把回包先清空，等待本次响应
            statusCommandFeedback = string.Empty;

            // 注册一次性响应过滤器
            DataReceivedEventHandler outputFilter = null;
            DataReceivedEventHandler errorFilter = null;

            outputFilter = (s, e) => HandlePossibleResponse(e.Data, id, ref outputFilter, ref errorFilter);
            errorFilter = (s, e) => HandlePossibleResponse(e.Data, id, ref outputFilter, ref errorFilter);

            _linkProcess.OutputDataReceived += outputFilter;
            _linkProcess.ErrorDataReceived += errorFilter;

            // 发命令
            try { _linkProcess.StandardInput.WriteLine(cmdJson);
                label1.Visible = true;
            }
            catch { /* 进程已挂，忽略 */ }
        }

        /// <summary>
        /// 只在收到对应 id 的响应时解析，解析完立即卸掉过滤器
        /// </summary>
        private void HandlePossibleResponse(string line,
                                            string expectId,
                                            ref DataReceivedEventHandler outputFilter,
                                            ref DataReceivedEventHandler errorFilter)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            try
            {
                dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(line);
                if (obj == null || obj.id == null || (string)obj.id != expectId) return;

                // 命中本次等待的 id
                bool ok = obj.success != null && (bool)obj.success;
                string msg = obj.msg?.ToString() ?? string.Empty;
                statusCommandFeedback = msg ?? string.Empty;
                label1.Text = $"工作模式:{msg}";
                // 卸掉过滤器，避免累积
                _linkProcess.OutputDataReceived -= outputFilter;
                _linkProcess.ErrorDataReceived -= errorFilter;
            }
            catch { /* 非 JSON 行，忽略 */ }
        }
        /// <summary>
        /// 外部调用：停止状态轮询
        /// </summary>
        public void StopStatusPolling()
        {
            if (_statusTimer != null)
            {
                _statusTimer.Stop();
                _statusTimer.Dispose();
                _statusTimer = null;
                label1.Visible = false;
            }
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            materialSingleLineTextField1.Visible = true;button2.Visible = true;
        }
        private void materialSingleLineTextField2_LostFocus(object sender, EventArgs e)
        {
            {
                // 获取文本框中的内容并赋值给变量提示码
                string text1 = materialSingleLineTextField2.Text;
                if (string.IsNullOrWhiteSpace(text1) || text1.Equals("输入提示码"))
                {
                    AntdUI.Message.warn(Program.MainForm, "你好像什么都没有写哦(提示码)", autoClose: 5, font: Program.AlertFont);
                    materialSingleLineTextField2.Text = "输入提示码";
                }
            }
        }

        private void materialSingleLineTextField2_Click(object sender, EventArgs e)
        {
            if (materialSingleLineTextField2.Text.Equals("输入提示码"))
            {
                materialSingleLineTextField2.Text = "";
            }
        }
    }
}
