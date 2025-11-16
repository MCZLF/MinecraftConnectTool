using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;

namespace MinecraftConnectTool
{
    public partial class ZTLobby : UserControl
    {
        public ZTLobby()
        {
            InitializeComponent();
        }

        private static void log(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }
        private void ZTLobby_Load(object sender, EventArgs e)
        {

        }

        private async void create_Click(object sender, EventArgs e)
        {
            try
            {
                // 构造匿名对象
                var dto = new
                {
            	    Machine   = Environment.MachineName,
                    NetName = networkname.Text.Trim(),
                    Cidr = dhcp.Text.Trim(),
                    Password = pass.Text.Trim(),
                    Lease = 0,
                    Broadcast = false
                };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);

                TcpClient tcp = new TcpClient();
                await tcp.ConnectAsync("mctservice.mczlf.loft.games", 17502);
                NetworkStream stream = tcp.GetStream();
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(buffer, 0, buffer.Length);

                StringBuilder sb = new StringBuilder();
                byte[] recv = new byte[1024];
                int len;
                while ((len = await stream.ReadAsync(recv, 0, recv.Length)) > 0)
                {
                    sb.Append(Encoding.UTF8.GetString(recv, 0, len));
                    if (sb.ToString().EndsWith("\r\n")) break;
                }
                tcp.Close();

                string resp = sb.ToString().TrimEnd('\r', '\n');
                Clipboard.SetText(resp);
                MessageBox.Show("服务器返回：\n" + resp, "创建结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region MD5校验
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
        #endregion

        //candy正常逻辑
        private async void createroom_Click(object sender, EventArgs e)
        {
            string tempDirectory = Path.GetTempPath();
            string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
            string rn = roomname.Text.Trim();
            string rp = roompwd.Text.Trim();

            if (string.IsNullOrEmpty(rn))
            {
                MessageBox.Show("房间名称不能为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dto = new
            {
                Machine = Environment.MachineName,
                NetName = rn,
                Cidr = "10.0.0.1/24",
                Password = rp,
                Lease = 0,
                Broadcast = false
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);

            try
            {
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    await tcp.ConnectAsync("mctservice.mczlf.loft.games", 17502);
                    using (var stream = tcp.GetStream())
                    {
                        byte[] buf = System.Text.Encoding.UTF8.GetBytes(json);
                        await stream.WriteAsync(buf, 0, buf.Length);

                        var sb = new System.Text.StringBuilder();
                        byte[] recv = new byte[1024];
                        int len;
                        while ((len = await stream.ReadAsync(recv, 0, recv.Length)) > 0)
                        {
                            sb.Append(System.Text.Encoding.UTF8.GetString(recv, 0, len));
                            if (sb.ToString().EndsWith("\r\n")) break;
                        }

                        string resp = sb.ToString().TrimEnd('\r', '\n');
                        dynamic r = Newtonsoft.Json.JsonConvert.DeserializeObject(resp);

                        switch ((int)r.status)
                        {
                            case 0:
                                MessageBox.Show("房间创建成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                            case 9:
                                MessageBox.Show("房间名称已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                break;
                            case 13:
                                MessageBox.Show("房间名称不合法", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                break;
                            default:
                                MessageBox.Show($"创建失败：{r.msg}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("网络错误：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //校验核心
            string fileName = Path.Combine(customDirectory,"CandyCore", "candy.exe");
            string fileNameExtra = Path.Combine(customDirectory, "Extra.exe");
            string fileMd5 = "3d015b60760d73390ec4b92dad371068";
            bool needsDownload = false;
            if (File.Exists(fileName))
            {
                string md5Hash = GetFileMD5Hash(fileName);
                if (md5Hash == fileMd5)
                {
                    log("64位核心已存在且安全校验通过");
                }
                else
                {log("核心不存在或安全校验不通过,重新Download中");
                needsDownload = true;}
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
                string url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/CandyCore.exe";
                using (var unityClient = new HttpClient())
                using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(fileNameExtra, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
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
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MCZLFAPP", "Temp", "Extra.exe"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    proc.OutputDataReceived += (s, args) =>
                    {
                        if (args.Data != null && args.Data.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
                            log("success");
                    };
                    proc.ErrorDataReceived += (s, args) =>
                    {
                        if (args.Data != null && args.Data.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
                            log("success");
                    };

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
            }
            //调用candy启动
            string workDir = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "CandyCore");
            string candyPath = Path.Combine(workDir, "candy.exe");

            string arguments = $"--mode client " +
                               $"--websocket wss://canets.org/linfon18/{rn} " +
                               $"--password {rp} " +
                               $"--name MCTAdaptor " +
                               $"--tun 10.0.0.1/24";

            richTextBox1.Clear();

            var psi2 = new ProcessStartInfo
            {
                FileName = candyPath,
                Arguments = arguments,
                WorkingDirectory = workDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (var proc = new Process { StartInfo = psi2, EnableRaisingEvents = true })
                {
                    proc.OutputDataReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                            this.Invoke((MethodInvoker)(() =>
                            {
                                richTextBox1.AppendText(args.Data + Environment.NewLine);
                            }));
                    };

                    proc.ErrorDataReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                            this.Invoke((MethodInvoker)(() =>
                            {
                                richTextBox1.AppendText(args.Data + Environment.NewLine);
                            }));
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    await Task.Run(() => proc.WaitForExit());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
    }

        private async void joinroom_Click(object sender, EventArgs e)
        {
            string tempDirectory = Path.GetTempPath();
            string customDirectory = Path.Combine(tempDirectory, "MCZLFAPP", "Temp");
            string rn = roomnamejoin.Text.Trim();
            string rp = roompwdjoin.Text.Trim();
            string dhcp = dhcpjoin.Text.Trim();          
            //校验核心
            string fileName = Path.Combine(customDirectory, "CandyCore", "candy.exe");
            string fileNameExtra = Path.Combine(customDirectory, "Extra.exe");
            string fileMd5 = "3d015b60760d73390ec4b92dad371068";
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
                    log("核心不存在或安全校验不通过,重新Download中");
                    needsDownload = true;
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
                string url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/CandyCore.exe";
                using (var unityClient = new HttpClient())
                using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(fileNameExtra, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
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
                // 直接替换你原来的那句 Process.Start(...)
                var psi2 = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MCZLFAPP", "Temp", "Extra.exe"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var proc = System.Diagnostics.Process.Start(psi2);
                if (proc != null)
                {
                    proc.OutputDataReceived += (s, args) =>
                    {
                        if (args.Data != null && args.Data.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
                            log("success");
                    };
                    proc.ErrorDataReceived += (s, args) =>
                    {
                        if (args.Data != null && args.Data.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
                            log("success");
                    };

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
            }
            if (string.IsNullOrEmpty(rn))
            {
                MessageBox.Show("房间名称不能为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(dhcp))
                dhcp = "10.0.0.1/24";                    // 默认兜底

            string workDir = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "CandyCore");
            string candyPath = Path.Combine(workDir, "candy.exe");

            string arguments = $"--mode client " +
                               $"--websocket wss://canets.org/linfon18/{rn} " +
                               $"--password {rp} " +
                               $"--name MCTAdaptor " +
                               $"--tun {dhcp}";

            richTextBox1.Clear();

            var psi = new ProcessStartInfo
            {
                FileName = candyPath,
                Arguments = arguments,
                WorkingDirectory = workDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (var proc = new Process { StartInfo = psi, EnableRaisingEvents = true })
                {
                    proc.OutputDataReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                            this.Invoke((MethodInvoker)(() => richTextBox1.AppendText(args.Data + Environment.NewLine)));
                    };

                    proc.ErrorDataReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                            this.Invoke((MethodInvoker)(() => richTextBox1.AppendText(args.Data + Environment.NewLine)));
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    await Task.Run(() => proc.WaitForExit());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var proc in Process.GetProcessesByName("candy"))
            {
                try
                {
                    proc.Kill();
                }
                catch { /* 忽略无权结束的进程 */ }
            }
            richTextBox1.AppendText("已关闭所有 candy.exe 进程" + Environment.NewLine);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/CandyCore.exe");
            Program.alertwarn("由于 Gitee 反爬虫策略，该文件无法自动下载。\n请手动下载后，【右键→以管理员模式运行】CandyCore.exe。");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("https://mct.mczlf.loft.games/quick-start/candyhelp");
        }
    }
}