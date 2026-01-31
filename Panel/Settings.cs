using AntdUI;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace MinecraftConnectTool
{
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            InitializeDropdown();
        }
        #region 字体
        private int correctAnswer;
        public static Font AlertFont { get; } = new Font("Microsoft YaHei UI", 8.3f);
        public static Font P2PFont { get; } = new Font("Microsoft YaHei UI", 9f);
        #endregion

        #region 人机验证部分
        private void button5_Click(object sender, EventArgs e)
        {
            button5.Loading = true;
            GenerateMathProblem();

            // 用新的 InteractiveModal 弹窗获取用户输入
            var customModal = new InteractiveModal();
            string userInput = customModal.ShowModal(Program.MainForm, "请输入验证码：");

            if (string.IsNullOrEmpty(userInput))
            {
                MessageBox.Show("未输入答案，无法上传日志！");
                button5.Loading = false;
                return;
            }

            Task.Run(async () =>
            {
                bool isValid = await ValidateVerificationCodeAsync(userInput);
                if (isValid)
                {
                    await UploadLogAsync();   // 新版上传逻辑（见下方）
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("验证码错误");
                        button5.Loading = false;
                    }));
                }
            });
        }
        private void GenerateMathProblem()
        {
            Random random = new Random();
            int num1 = random.Next(1, 10); // 生成一个1到9的随机数
            int num2 = random.Next(1, 10); // 生成一个1到9的随机数
            char operation = (random.Next(2) == 0) ? '+' : '-';
            if (operation == '-' && num1 < num2)
            {
                int temp = num1;
                num1 = num2;
                num2 = temp;
            }
            correctAnswer = operation == '+' ? num1 + num2 : num1 - num2;
            MessageBox.Show($"{num1} {operation} {num2} = ?");
        }
        private async Task<bool> ValidateVerificationCodeAsync(string userInput)
        {
            await Task.Delay(100);
            if (int.TryParse(userInput, out int userAnswer))
            {
                return userAnswer == correctAnswer;
            }
            return false;
        }
        #endregion
        // 上传日志的方法
        private async Task UploadLogAsync()
        {
            const string serverHost = "mctservice.mczlf.loft.games";
            const int serverPort = 17500;
            const int timeoutMs = 5000;

            string failReason = null;

            try
            {
                // 1. 日志文件
                string logPath = Path.Combine(
                    Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                    "MCZLFAPP", "Temp", "log", "openp2p.log");

                if (!File.Exists(logPath)) { failReason = "日志文件不存在！"; return; }
                string logContent = File.ReadAllText(logPath, Encoding.UTF8);

                // 2. 系统信息（保持原样）
                string curlVer = "N/A";
                try
                {
                    using (Process p = new Process())
                    {
                        p.StartInfo.FileName = "curl";
                        p.StartInfo.Arguments = "--version";
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        curlVer = p.StandardOutput.ReadLine() ?? "N/A";
                        p.WaitForExit();
                    }
                }
                catch { }

                // 3. 构造 JSON（保持原样）
                var dto = new
                {
                    Time = DateTime.Now,
                    MachineName = Environment.MachineName,
                    Content =
        $@"UploadTime{DateTime.Now}
===Curl版本===
{curlVer}

===Windows版本===
{Environment.OSVersion}

===openp2p.log===
{logContent}"
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
                byte[] payload = Encoding.UTF8.GetBytes(json);

                // 4. 连接 + 发送 + 接收（.NET 4.7.2 兼容）
                using (var client = new TcpClient())
                {
                    // 确保发送完立即发送 FIN
                    client.LingerState = new LingerOption(true, 0);

                    var connectTask = client.ConnectAsync(serverHost, serverPort);
                    bool ok = await Task.WhenAny(connectTask, Task.Delay(timeoutMs)) == connectTask;
                    if (!ok) { failReason = "连接服务器超时"; return; }

                    var stream = client.GetStream();
                    stream.WriteTimeout = timeoutMs;
                    stream.ReadTimeout = timeoutMs;

                    await stream.WriteAsync(payload, 0, payload.Length);

                    using (var ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[256];
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            ms.Write(buffer, 0, read);

                        string resp = Encoding.UTF8.GetString(ms.ToArray()).Trim();

                        if (resp.StartsWith("LIMIT", StringComparison.OrdinalIgnoreCase))
                            failReason = "当前 IP 本小时上传次数已达上限";
                        else if (resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                            failReason = null; // 成功
                        else
                            failReason = $"服务器返回未知信息：{resp}";
                    }
                }
            }
            catch (SocketException) { failReason = "网络连接失败"; }
            catch (Exception ex) { failReason = $"上传失败：{ex.Message}"; }
            finally
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    button5.Loading = false;
                    if (string.IsNullOrEmpty(failReason))
                        MessageBox.Show("上传成功！", "上传日志", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show(failReason, "上传日志", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
            }
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Loading = true;
            string url = "https://gitee.com/linfon18/SunLogin2/raw/master/360DNS.exe";
            string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "MCZLFAPP", "Temp");
            string destinationPath = Path.Combine(tempDir, "360DNS.exe");
            string expectedMd5 = "a0c67c45b118e9706cadb771b3014528";
            bool needsDownload = false;
            // 确保目标文件夹存在
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            if (File.Exists(destinationPath))
            {
                string md5Hash = GetFileMD5Hash(destinationPath);
                if (md5Hash == expectedMd5)
                {
                }
                else
                {
                    needsDownload = true;
                }
            }
            else
            {
                needsDownload = true;
            }

            if (needsDownload)
            {
                try
                {
                    progress1.ShowInTaskbar = true;
                    progress1.Visible = true; // 开始下载时显示进度条
                    progress1.Value = 0; // 初始化进度条
                    using (var unityClient = new HttpClient())
                    {
                        using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            long totalBytes = response.Content.Headers.ContentLength ?? 0;
                            long downloadedBytes = 0;

                            using (var httpStream = await response.Content.ReadAsStreamAsync())
                            {
                                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
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
                        }
                    }
                    progress1.Visible = false;
                    progress1.Value = 0;
                    progress1.ShowInTaskbar = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    progress1.Visible = false;
                    progress1.Value = 0;
                    progress1.ShowInTaskbar = false;
                    button10.Loading = false;
                    return;
                }
            }
            progress1.Visible = false;
            progress1.Value = 0;
            progress1.ShowInTaskbar = false;

            if (File.Exists(destinationPath))
            {
                try
                {
                    button1.Loading = false;
                    Process.Start(destinationPath);
                }
                catch (System.ComponentModel.Win32Exception win32Exception)
                {
                    button1.Loading = false;
                    MessageBox.Show($"无法启动程序：{win32Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    button1.Loading = false;
                }
            }
        }
        public static string GetFileMD5Hash(string filePath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    MD5 md5 = MD5.Create();
                    byte[] hashValue = md5.ComputeHash(stream);

                    // 将字节数组转换为十六进制字符串
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
                throw new Exception("Error computing MD5 hash for file " + filePath, ex);
            }
        }

        private void alert1_Click(object sender, EventArgs e)
        {
            var x = Form1.designation;
            badge2.Enabled = true;
            button13.Enabled = true;
            AntdUI.Message.error(Program.MainForm, "回声洞已于2025.3.15日下线，仅0.0.5或更低版本支持", autoClose: 5);
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"特殊编译：否\nP2P版本:3.24.23(房主)\n3.24.13(玩家)\n\n版本代号 {x}", AntdUI.TType.Info)
                {
                    OnButtonStyle = (id, btn) =>
                    {
                        btn.BackExtend = "135, #6253E1, #04BEFE";
                    },
                    CancelText = null,
                    OkText = "知道了"
                });
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, "确认操作", "清除缓存会同时清理掉已有配置文件\n且清除后部分文件需要重新下载，是否继续？", TType.Info)
            {
                Font = P2PFont,
                OkText = "确认",
                CancelText = null,
                OnOk = config =>
                {
                    DeleteTempFolderContents();
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
                        //no
                        return true;
                    }
                    return false; // 返回 false 表示不关闭弹窗，true 表示关闭弹窗
                }
            });
        }
        private void DeleteTempFolderContents()
        {
            try
            {
                string tempPath = Environment.GetEnvironmentVariable("TEMP");
                string targetFolder = Path.Combine(tempPath, "MCZLFAPP");

                // 检查目标文件夹是否存在
                if (Directory.Exists(targetFolder))
                {
                    foreach (string file in Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(file, FileAttributes.Normal); // 确保文件不是只读
                        File.Delete(file);
                    }

                    foreach (string folder in Directory.GetDirectories(targetFolder, "*.*", SearchOption.AllDirectories))
                    {
                        Directory.Delete(folder, true);
                    }
                    AntdUI.Message.success(Program.MainForm, $"清除成功~", autoClose: 5, font: P2PFont);
                    Application.Restart();
                }
                else
                {
                    AntdUI.Message.info(Program.MainForm, $"缓存为空,无需清理哦~", autoClose: 5, font: P2PFont);
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(Program.MainForm, $"清除失败,请手动清除,原因:" + ex.Message, autoClose: 5, font: P2PFont);
            }
        }
        private void DeleteConfig()
        {
            try
            {
                string tempPath = Environment.GetEnvironmentVariable("TEMP");
                string targetIniPath = Path.Combine(tempPath, "MCZLFAPP", "Temp", "APPconfig.json");
                if (File.Exists(targetIniPath))
                {
                    File.SetAttributes(targetIniPath, FileAttributes.Normal);
                    File.Delete(targetIniPath);
                    AntdUI.Message.success(Program.MainForm, "[APPConfig]配置文件删除成功~", autoClose: 5, font: P2PFont);
                    Application.Restart();
                }
                else
                {
                    AntdUI.Message.info(Program.MainForm, "[APPCONFIG]配置文件不存在,无需删除哦~", autoClose: 5, font: P2PFont);
                }
                string targetThemePath = Path.Combine(tempPath, "MCZLFAPP", "theme.json");
                if (File.Exists(targetIniPath))
                {
                    File.SetAttributes(targetThemePath, FileAttributes.Normal);
                    File.Delete(targetThemePath);
                    AntdUI.Message.success(Program.MainForm, "[Theme]配置文件删除成功~", autoClose: 5, font: P2PFont);
                    Application.Restart();
                }
                else
                {
                    AntdUI.Message.info(Program.MainForm, "[Theme]配置文件不存在,无需删除哦~", autoClose: 5, font: P2PFont);
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(Program.MainForm, $"[Theme]删除配置文件失败,请手动删除,原因:{ex.Message}", autoClose: 5, font: P2PFont);
            }
        }
        private void InitializeDropdown()
        {
            int maxConnections = Form1.config.read<int>("Bar", 1);
            dropdown1.Items.Clear();
            dropdown1.Items.AddRange(new AntdUI.SelectItem[]
            {
                new AntdUI.SelectItem("实时网速"), // 默认选中项
                new AntdUI.SelectItem("天气"),
                new AntdUI.SelectItem("无"),
            });
            dropdown1.SelectedValueChanged += Dropdown1_SelectedValueChanged;
        }

        private void Dropdown1_SelectedValueChanged(object sender, AntdUI.ObjectNEventArgs e)
        {
            var selectedValue = e.Value.ToString();
            switch (selectedValue)
            {
                case "实时网速":
                    Form1.config.write("Bar", 1);
                    dropdown1.Text = "实时网速";
                    badge1.State = TState.Success;
                    AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: P2PFont);
                    break;
                case "天气":
                    Form1.config.write("Bar", 2);
                    dropdown1.Text = "天气";
                    badge1.State = TState.Success;
                    AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: P2PFont);
                    break;
                case "无":
                    Form1.config.write("Bar", 0);
                    dropdown1.Text = "无";
                    badge1.State = TState.Default;
                    AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: P2PFont);
                    break;
                default:
                    // 如果选中的是默认项或其他项，不做处理
                    break;
            }
        }

        private void dropdown1_SelectedValueChanged_1(object sender, ObjectNEventArgs e)
        {

        }
        //Set加载
        private void Settings_Load(object sender, EventArgs e)
        {
            color();
            bool EnableVersionCheck = Form1.config.read<bool>("EnableVersionCheck", true);
            if (EnableVersionCheck)
            {
                switch6.Checked = true;
            }
            bool datasaving = Form1.config.read<bool>("datasaving", false);
            if (datasaving)
            { switch7.Checked = true; }
                bool EnableServerPost = Form1.config.read<bool>("ServerPostEnable", true);
            if (EnableServerPost)
            {
                switch2.Checked = true;
            }
            bool getpreupdate = Form1.config.read<bool>("getpreupdate", false);
            if (getpreupdate)
            {
                switch3.Checked = true;
            }
            bool nonotifywhenstart = Form1.config.read<bool>("nonotifywhenstart", false);
            if (nonotifywhenstart)
            {
                switch4.Checked = true;
            }
            //bool usecustomport = Form1.config.read<bool>("usecustomport", false);
            //if (usecustomport)
            //{
            //    string customport = Form1.config.read<string>("customport", "不存在");
            //    switch1.Checked = true;
            //    input1.Text = customport;
            //    input1.Visible = true;
            //}
            //bool usecustomnode = Form1.config.read<bool>("usecustomnode", false);
            //if (usecustomnode)
            //{
            //    string customnode = Form1.config.read<string>("customnode", "不存在");
            //    switch5.Checked = true;
            //    input2.Text = customnode;
            //    input2.Visible = true;
            //}
            bool showp2pbug = Form1.config.read<bool>("ShowP2PBug", false);
            if (showp2pbug)
            {
                checkbox2.Checked = true;
            }
            bool strupd = Form1.config.read<bool>("goupdatewhenstart", false);
            if (strupd)
            {
                checkbox1.Checked = true;
            }
            int barload = Form1.config.read<int>("Bar", 1);
            if (barload == 1)
            {
                dropdown1.Text = "实时网速";
                badge1.State = TState.Success;
            }
            else if (barload == 2)
            {
                dropdown1.Text = "天气";
                badge1.State = TState.Success;
            }
            else if (barload == 3)
            {
                dropdown1.Text = "无";
                badge1.State = TState.Default;
            }
            else
            {
                AntdUI.Message.error(Program.MainForm, $"[调试信息]Bar = {barload}，Bar不显示", autoClose: 5, font: P2PFont);
                badge1.State = TState.Error;
            }
            color();
        }

        private void checkbox1_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (checkbox1.Checked)
            {
                Form1.config.write("goupdatewhenstart", true);
                checkbox1.Checked = true;
            }
            else
            {
                Form1.config.write("goupdatewhenstart", false);
                checkbox1.Checked = false;
            }
        }

        private void badge1_Click(object sender, EventArgs e)
        {

        }

        private void checkbox2_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (checkbox2.Checked)
            {
                Form1.config.write("ShowP2PBug", true);
                checkbox2.Checked = true;
            }
            else
            {
                Form1.config.write("ShowP2PBug", false);
                checkbox2.Checked = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, "确认操作", "此操作将会重置配置文件,恢复默认设置\n不影响已下载的核心,确定继续咩？", TType.Info)
            {
                Font = P2PFont,
                OkText = "确认",
                CancelText = null,
                OnOk = config =>
                {
                    DeleteConfig();
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
                        return true;
                    }
                    return false;
                }
            });
        }

        private void switch1_CheckedChanged(object sender, BoolEventArgs e)
        {
            savebut.Loading = false;
            if (switch1.Checked)
            {
                input1.Visible = true;
                switch1.Checked = true;
            }
            else
            {
                savebut.Visible = true;
                input1.Visible = false;
                switch1.Checked = false;
            }
        }

        private void savebut_Click(object sender, EventArgs e)
        {
            savebut.Loading = true;
            string customport = input1.Text;
            if (switch1.Checked == true)
            {
                if (int.TryParse(customport, out int port) && port >= 1 && port <= 65535)
            {
                Form1.config.write("usecustomport", true);
                Form1.config.write("customport", customport);
            }
            else
            {
                AntdUI.Message.error(Program.MainForm, "请输入有效的端口(1-65535)", autoClose: 5,font: P2PFont);
                Form1.config.write("usecustomport", false);
            }
            }
            if (switch1.Checked == false)
            {
                Form1.config.delete("customport");
                Form1.config.write("usecustomport", false);
            }
            savebut.Visible = false;
        }

        private void input1_TextChanged(object sender, EventArgs e)
        {
            string customportc = Form1.config.read<string>("customport", "不存在");
            if (customportc == input1.Text)
            {
                savebut.Visible = false;
            }
            else
            {
                Form1.config.delete("customport");
                savebut.Loading = false;
                savebut.Visible = true;
            }
        }


        private void switch2_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (switch2.Checked)
            {
                Form1.config.write("ServerPostEnable", true);
                switch2.Checked = true;
            }
            else
            {
                Form1.config.write("ServerPostEnable", false);
                switch2.Checked = false;
            }
        }
        
        private void button4_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", "开启后将允许加入方在加入后，在[多人游戏-居于网世界]中自动搜索到房间信息\n可能存在未被发现并修复的问题,如有遇到欢迎反馈\nMinecraft Java Edition 1.9+ 支持该功能 \n该选项已于0.0.6.091(SP1测试版本)中更新为自动抉择模式", AntdUI.TType.Warn)
            {
                CloseIcon = true,
                Font = P2PFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }

        private void switch3_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (switch3.Checked)
            {
                Form1.config.write("getpreupdate", true);
                switch3.Checked = true;
            }
            else
            {
                Form1.config.write("getpreupdate", false);
                switch3.Checked = false;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", "接收最新的构建版本更新\n可能存在未被发现并修复的问题,如有遇到欢迎反馈\n新版本,启动！", AntdUI.TType.Info)
            {
                CloseIcon = true,
                Font = P2PFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }

        private void button7_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info\n当前功能尚在测试中", "通过强制中转优化体验\n可能存在未被发现并修复的问题,如有遇到欢迎反馈\nPS:每次启动时会默认将此配置项设置为false,因为在测试\n服务器捐助:mczlf@qq.com", AntdUI.TType.Warn)
            {
                CloseIcon = true,
                Font = P2PFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }

        private void button8_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", "自定义加入方的端口\n开启后加入ip将在下一次启动时更新为 127.0.0.1:自定义端口", AntdUI.TType.Info)
            {
                CloseIcon = true,
                Font = P2PFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string tempPath = Environment.GetEnvironmentVariable("TEMP");
            string folderPath = System.IO.Path.Combine(tempPath, "MCZLFAPP");
            string command = $"Add-MpPreference -ExclusionPath \"{folderPath}\"";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", $"成功将{folderPath}添加到Windows Defender排除列表中\n可自行打开Windows Defender查看\n", AntdUI.TType.Info)
            {
                CloseIcon = true,
                Font = P2PFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }

        private void switch4_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (switch4.Checked)
            {
                Form1.config.write("nonotifywhenstart", true);
                switch4.Checked = true;
            }
            else
            {
                Form1.config.write("nonotifywhenstart", false);
                switch4.Checked = false;
            }
        }

        private async void button10_Click(object sender, EventArgs e)
        {
            button10.Loading = true;
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"注意事项\n请将弹出的连接码和验证码,【截图】发送给可信的管理员或开发者", "请谨慎协助方身份,切勿随意发送给未知人员\n使用后请在托盘内退出!!!!!!,请勿保留后台", AntdUI.TType.Error)
            {
                CloseIcon = true,
                Font = AlertFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
            string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "MCZLFAPP", "Temp");
            string destinationPath;
            string url;
            string expectedMd5;
            bool needsDownload = false;

            if (Environment.Is64BitOperatingSystem)
            {
                url = "https://gitee.com/linfon18/SunLogin2/releases/download/64/SunLogin64.exe";
                expectedMd5 = "e31cb2f51ebbcc98ca9f51645727eb00";
                destinationPath = Path.Combine(tempDir, "SunLogin64.exe");
            }
            else
            {
                url = "https://gitee.com/linfon18/SunLogin2/releases/download/3264/SunLogin32.exe";
                expectedMd5 = "55726ad06d8ad4484210345b195d285a";
                destinationPath = Path.Combine(tempDir, "SunLogin32.exe");
            }
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            if (File.Exists(destinationPath))
            {
                string md5Hash = GetFileMD5Hash(destinationPath);
                if (md5Hash != expectedMd5)
                {
                    needsDownload = true;
                }
            }
            else
            {
                needsDownload = true;
            }
            if (needsDownload)
            {
                try
                {
                    progress1.ShowInTaskbar = true;
                    progress1.Visible = true; // 开始下载时显示进度条
                    progress1.Value = 0; // 初始化进度条
                    using (var unityClient = new HttpClient())
                    {
                        using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            long totalBytes = response.Content.Headers.ContentLength ?? 0;
                            long downloadedBytes = 0;

                            using (var httpStream = await response.Content.ReadAsStreamAsync())
                            {
                                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
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
                        }
                    }
                    progress1.Visible = false; 
                    progress1.Value = 0;
                    progress1.ShowInTaskbar = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    progress1.Visible = false;
                    progress1.Value = 0;
                    progress1.ShowInTaskbar = false;
                    button10.Loading = false;
                    return;
                }
            }
            progress1.Visible = false;
            progress1.Value = 0;
            progress1.ShowInTaskbar = false;
            if (File.Exists(destinationPath))
            {
                try
                {
                    Process.Start(destinationPath);
                    button11.Visible = true;
                }
                catch (System.ComponentModel.Win32Exception win32Exception)
                {
                    MessageBox.Show($"无法启动程序：{win32Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            button10.Loading = false;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                string killCommand32 = "taskkill /IM SunLogin32.exe /F";
                string killCommand64 = "taskkill /IM SunLogin64.exe /F";
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {killCommand32} & {killCommand64}",
                    Verb = "runas", 
                    CreateNoWindow = true, 
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
                button11.Visible = false;
                AntdUI.Message.info(Program.MainForm, "已尝试关闭,但由于权限问题,如果未能够关闭请在任务栏托盘处关闭", autoClose: 5, font: AlertFont);
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(Program.MainForm, $"发生错误：{ex.Message}", autoClose: 5, font: AlertFont);
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            AntdUI.Drawer.open(new AntdUI.Drawer.Config(Program.MainForm, new compatibility() { Size = new Size(307, 455) })
            {
                Align = TAlignMini.Right, 
                Mask = true, 
                MaskClosable = true, 
                DisplayDelay = 0
            });
            return;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            AntdUI.Message.warn(Program.MainForm, "这是一个测试功能.包含诸多不确定性\n请勿依赖此功能,启用后仍然直连优先", autoClose: 5, font: AlertFont);
            AntdUI.Drawer.open(new AntdUI.Drawer.Config(Program.MainForm, new relayserver() { Size = new Size(307, 455) })
            {
                Align = TAlignMini.Right,
                Mask = true,
                MaskClosable = true,
                DisplayDelay = 0
            });
            return;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, "嗯..需要说个事", "赞助我你得不到任何东西,不赞助也不会影响使用,咱要继续么\n(ノω<。)ノ))☆.。", TType.Warn)
            {
                Font = new Font("Microsoft YaHei UI", 9f),
                OkText = "确认",
                CancelText = null,
                OnOk = config =>
                {
                    System.Diagnostics.Process.Start("https://afdian.com/a/linfon18/");
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
                        return true;
                    }
                    return false;
                }
            });
        }

        private void switch5_CheckedChanged(object sender, BoolEventArgs e)
        {
            //savebut2.Loading = false;
            //if (switch5.Checked)
            //{
            //    input2.Visible = true;
            //    switch5.Checked = true;
            //}
            //else
            //{
            //    savebut2.Visible = true;
            //    input2.Visible = false;
            //    switch5.Checked = false;
            //}
        }

        private void input2_TextChanged(object sender, EventArgs e)
        {
            string customportc = Form1.config.read<string>("customnode", "不存在");
            if (customportc == input2.Text)
            {
                savebut2.Visible = false;
            }
            else
            {
                Form1.config.delete("customnode");
                savebut2.Loading = false;
                savebut2.Visible = true;
            }
        }

        private async void savebut2_Click(object sender, EventArgs e)
        {
            savebut2.Loading = true;
            if (System.Text.RegularExpressions.Regex.IsMatch(input2.Text, @"[^\u4e00-\u9fa5a-zA-Z0-9\-]"))
            {
                AntdUI.Message.error(Program.MainForm, "提示码仅可输入汉字、数字、字母或-连字符", autoClose: 5, font: P2PFont);
                //savebut2.Visible = false;
                savebut2.Loading = false;
                return;
            }
            // 敏感词检测
            if (switch5.Checked && input2.Text.Length >= 8)
            {
                try
                {
                    var o = Newtonsoft.Json.Linq.JObject.Parse(
                        await new System.Net.Http.HttpClient().GetStringAsync(
                            $"https://uapis.cn/api/prohibited?text={System.Uri.EscapeDataString(input2.Text)}"));
                    if ((string)o["status"] == "forbidden")
                    {
                        AntdUI.Message.error(Program.MainForm,
                            $"包含敏感词：{(string)o["forbiddenWord"]}", autoClose: 5, font: P2PFont);
                        return;    
                    }
                }
                catch
                {
                    AntdUI.Message.error(Program.MainForm,$"网络异常", autoClose: 5, font: P2PFont); return; }

                Form1.config.write("usecustomnode", true);
                Form1.config.write("customnode", input2.Text);
            }
            else if (switch5.Checked)
            {
                AntdUI.Message.error(Program.MainForm, "提示码过段,需要至少8位", autoClose: 5, font: P2PFont);
                Form1.config.write("usecustomnode", false);
            }
            else
            {
                Form1.config.delete("customnode");
                Form1.config.write("usecustomnode", false);
            }

            savebut2.Visible = false;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            AntdUI.Drawer.open(new AntdUI.Drawer.Config(Program.MainForm, new AdvancedSettings() { Size = new Size(307, 455) })
            {
                Align = TAlignMini.Right,
                Mask = true,
                MaskClosable = true,
                DisplayDelay = 0
            });
        }

        private void button18_Click(object sender, EventArgs e)
        {
            AntdUI.Drawer.open(new AntdUI.Drawer.Config(Program.MainForm, new InviteinfoEdit() { Size = new Size(307, 455) })
            {
                Align = TAlignMini.Right,
                Mask = true,
                MaskClosable = true,
                DisplayDelay = 0
            });
            return;
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button15_Click(object sender, EventArgs e)
        {

        }

        // button_mclojgs 的 Click 事件（双击按钮即可自动生成）
        private async void button_mclogs_Click(object sender, EventArgs e)
        {
            button_mclogs.Loading = true;
            const string api = "https://api.mclo.gs/1/log";
            try
            {
                string logPath = Path.Combine(
                    Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                    "MCZLFAPP", "Temp", "log", "openp2p.log");

                if (!File.Exists(logPath))
                {
                    MessageBox.Show("P2P日志文件不存在！", "云日志上传", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string logContent = File.ReadAllText(logPath, Encoding.UTF8);

                string body =
        $@"UploadTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
MachineName: {Environment.MachineName}
Version: {Form1.version}
===openp2p.log===
{logContent}";

                using (var hc = new HttpClient())
                {
                    var content = new StringContent("content=" + Uri.EscapeDataString(body),
                                                    Encoding.UTF8,
                                                    "application/x-www-form-urlencoded");

                    string resp = await hc.PostAsync(api, content)
                                          .ContinueWith(t => t.Result.Content.ReadAsStringAsync().Result);

                    dynamic json = JsonConvert.DeserializeObject(resp);
                    if (!(bool)json.success)
                    {
                        MessageBox.Show((string)json.error, "云日志上传", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string url = (string)json.url;
                    Clipboard.SetText(url);
                    MessageBox.Show($"上传成功！\n日志地址：{url}\n已复制入剪切板中,可直接粘贴使用", "云日志上传", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    button_mclogs.Loading = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"上传失败：{ex.Message}", "云日志上传", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button_mclogs.Loading = false;
            }
        }

        private void switch6_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (switch6.Checked)
            {
                Form1.config.write("EnableVersionCheck", true);
                switch6.Checked = true;
            }
            else
            {
                Form1.config.write("EnableVersionCheck", false);
                switch6.Checked = false;
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            AntdUI.Drawer.open(new AntdUI.Drawer.Config(Program.MainForm, new Theme() { Size = new Size(730, 445) })
            {
                Align = TAlignMini.Right,
                Mask = true,
                MaskClosable = true,
                DisplayDelay = 0
            });
        }
        //colorUI
        static void ApplyColor(string key, Action<Color> setColor)
        {
            var hex = ThemeConfig.ReadHex(key);
            if (hex == null) return;

            var c = ColorTranslator.FromHtml(hex);
            if (c.A != 255) return;   // 透明色拒绝刷写

            setColor(c);
        }
        private void color()
        {
            bool EnableColor = Form1.config.read<bool>("EnableColor", false);
            if (EnableColor)
            {var imgPath = ThemeConfig.ReadString("BackPNGAdd");
            if (ThemeConfig.ReadBool("BackPNG") && File.Exists(imgPath))
            {
                try
                {
                    // 只读、不锁文件；按需缩放
                    using (var fs = new FileStream(imgPath, FileMode.Open, FileAccess.Read))
                    using (var src = Image.FromStream(fs))
                    {
                        // 缩到窗口大小（保持比例）
                        var sz = this.ClientSize;
                        this.BackgroundImage = new Bitmap(src, sz.Width, sz.Height);
                    }
                }
                catch { this.BackgroundImage = null; }
            }
            else
            {
                this.BackgroundImage = null;
                ApplyColor("P2PBack", c => this.BackColor = c);
            }
            foreach (var btn in this.Controls.OfType<AntdUI.Button>())//一次性将ANTDUI.BUTTON染色,不包含其他逻辑
            {
                ApplyColor("UpdateButton", c => btn.BackColor = c);
                ApplyColor("UpdateButton", c => btn.DefaultBack = c);
            }
            }
            
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void button19_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", "目前流量节省只对P2PMode生效，且具体生效情况尚在测试\n开启流量节省后，将禁止其他设备无关设备连接本机，也禁止本机连接其他中转\n可能会对联机造成一定影响,如果流量重组请勿开启此选项", AntdUI.TType.Info)
            {
                CloseIcon = true,
                Font = P2PFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }

        private void switch7_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (switch7.Checked)
            {
                Form1.config.write("datasaving", true);
                switch7.Checked = true;
            }
            else
            {
                Form1.config.write("datasaving", false);
                switch7.Checked = false;
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/MCZLF/MinecraftConnectTool");
        }
    }
}

