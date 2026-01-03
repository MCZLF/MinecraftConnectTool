using AntdUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MinecraftConnectTool
{
    public partial class Update : UserControl
    {
        public Update()
        {
            InitializeComponent();
        }
        string url;
        string urldownload;
        public static string urlupdatelog;

        private void Update_Load(object sender, EventArgs e)
        {
            LoadVersion();
            color();
            button3.Enabled = false;
            button1.PerformClick();
            bool EnableReceiveNMode = Form1.EnableReceiveNMode;
            bool getpreupdate = Form1.config.read<bool>("getpreupdate", false);
            if (getpreupdate)
            {
                try
                {
                    if(EnableReceiveNMode)
                    { bool contains = CheckName();
                    if (contains)
                    {
                        label4.Text = "当前更新通道：内测";
                        label4.PrefixSvg = "ExperimentOutlined";
                        url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/Preview/Version006N";
                        urlupdatelog = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/Preview/updatelog6N";
                        urldownload = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/Preview/NPreLaest.exe";
                    }}
                    else
                    {
                        label4.Text = "当前更新通道：测试";
                        label4.PrefixSvg = "ClockCircleOutlined";
                        url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/Preview/version006Pre";
                        urlupdatelog = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/Preview/PreUpdatelog006";
                        urldownload = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/Preview/PreLatest.exe";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("发生错误：" + ex.Message);
                }
            }
            else
            {
                url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/version006.txt";
                urlupdatelog = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/updatelog6";
                urldownload = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/Latest.exe";
            }
        }
        private string version;
        private string cloudV;
        private string updatelog;
        private void LoadVersion()
        {
            try{version = Form1.version;}
            catch (Exception ex)
            {
                AntdUI.Message.error(Program.MainForm, "获取当前版本时出错," + ex.Message, autoClose: 5, font: Program.AlertFont);
                version = "Unknown Version";
            }
            if (string.IsNullOrEmpty(version))
            {
                AntdUI.Message.error(Program.MainForm, "[异常]获取到的版本号为空", autoClose: 5, font: Program.AlertFont);
                version = "Unknown Version";
            }
            Debug.Print("GetVersion"+ version);
        }
        //checkupate
        private int clickCount = 0;
        private DateTime lastClickTime = DateTime.MinValue;
        private async void button1_Click(object sender, EventArgs e)
        {
            TimeSpan timeSinceLastClick = DateTime.Now - lastClickTime;
            if (timeSinceLastClick.TotalSeconds > 60)
            {
                clickCount = 0;
            }
            if (clickCount < 4)
            {
                clickCount++;
                lastClickTime = DateTime.Now;
                try
                {
                    //AntdUI.Message.info(Program.MainForm, $"调试信息：当前版本{version}", autoClose: 5, font: Program.AlertFont);
                    label2.Text = ("当前版本:" + version);
                    Debug.Print("当前版本" + version);
                    string cloudVersion = await FetchCloudVersionAsync();
                    cloudV = cloudVersion;
                    updatelog = await FetchUpdateLogAsync();
                    SetLog(updatelog);
                    label3.Text = ("云版本号:" + cloudVersion);
                    //校验
                    if (version == cloudVersion)
                    {
                        button3.Enabled = false;
                        button3.Text = ("已是最新版本");
                        button3.IconSvg = ("CheckCircleFilled");
                        AntdUI.Message.info(Program.MainForm, $"当前已是最新版本", autoClose: 5, font: Program.AlertFont);
                    }
                    else
                    {
                        alert2.Badge = " ";
                        button3.Enabled = true;
                        AntdUI.Message.info(Program.MainForm, $"发现新版本~(≧▽≦)/~\n新版本为{cloudVersion}", autoClose: 5, font: Program.AlertFont);
                    }
                }
                catch (Exception ex)
                {
                    AntdUI.Message.open(new AntdUI.Message.Config(Program.MainForm, $"发生错误: {ex.Message}", AntdUI.TType.Error)
                    {
                        Font = Program.AlertFont,
                        AutoClose = 5,
                        ClickClose = true
                    });
                }
            }
            else
            {
                AntdUI.Notification.error((Form)Program.MainForm, "Oops", "出了点小问题，请稍后重试", align: AntdUI.TAlignFrom.BR, font: Program.AlertFont);
            }
        }

        //切页
        private const int LINES_PER_PAGE = 17;
        private readonly List<string> _fullLogLines = new List<string>();
        private int _currentPage = 0;
        private int _pageCount = 0;
        public void SetLog(string logText)
        {
            //// 删除不需要的行
            //_fullLogLines.Clear();
            //_fullLogLines.AddRange(logText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            //                              .Where(l => !string.IsNullOrWhiteSpace(l)));

            _fullLogLines.AddRange(logText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));  //我要保留行
            // 计算总页数
            _pageCount = (int)Math.Ceiling((double)_fullLogLines.Count / LINES_PER_PAGE);

            // 回到首页并展示
            _currentPage = 0;
            ShowPage(_currentPage);
        }

        /// <summary>
        /// 展示指定页，并控制翻页按钮可见性
        /// </summary>
        private void ShowPage(int pageIndex)
        {
            // 容错
            if (_pageCount == 0)
            {
                alert2.Text = string.Empty;
                button4.Visible = button5.Visible = false;
                return;
            }

            // 合法页号范围
            pageIndex = Math.Max(0, Math.Min(pageIndex, _pageCount - 1));
            _currentPage = pageIndex;

            // 取 start~end 行
            int start = pageIndex * LINES_PER_PAGE;
            int end = Math.Min(start + LINES_PER_PAGE, _fullLogLines.Count);
            var pageLines = _fullLogLines.GetRange(start, end - start);

            // 贴到控件
            alert2.Text = string.Join(Environment.NewLine, pageLines);

            // 控制按钮
            button4.Visible = _currentPage > 0;               // 不是第一页就显示“上一页”
            button5.Visible = _currentPage < _pageCount - 1;  // 不是最后一页就显示“下一页”
        }

        // 上一页
        private void button4_Click(object sender, EventArgs e)
        {
            if (_currentPage > 0)
                ShowPage(_currentPage - 1);
        }

        // 下一页
        private void button5_Click(object sender, EventArgs e)
        {
            if (_currentPage < _pageCount - 1)
                ShowPage(_currentPage + 1);
        }



        // 异步方法获取云版本
        private async Task<string> FetchCloudVersionAsync()
        {
            await AntdUI.Spin.open(this, config =>
            {
                config.Text = "正在检查更新...";
                config.Color = Color.Blue;
                config.Fore = Color.White;
                config.Font = new Font("Arial", 12);
            });
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching version: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                await AntdUI.Spin.open(this, config => { }, end: () => { });
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            AntdUI.Label label = new AntdUI.Label
            {
                Text = "当前已是最新版本",
                Font = new Font("微软雅黑", 12, FontStyle.Bold),
                ForeColor = Color.Green,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(200, 30)
            };
            int x = (this.Width - label.Width) / 2;
            int y = (this.Height - label.Height) / 2;
            label.Location = new Point(x, y);
            this.Controls.Add(label);
            throw new Exception("114514,点继续就没事了");
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            string currentFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string currentDirectory = Path.GetDirectoryName(currentFilePath);
            string newFileName = $"MinecraftConnectTool_{cloudV}.exe";
            string newFilePath = Path.Combine(currentDirectory, newFileName);
            using (WebClient webClient = new WebClient())
            {
                try
                {
                    try
                    {
                        progress1.ShowInTaskbar = true;
                        progress1.Visible = true; // 开始下载时显示进度条
                        progress1.Value = 0; // 初始化进度条

                        using (var unityClient = new HttpClient())
                        {
                            //AntdUI.Message.success(Program.MainForm, $"urldownload {urldownload}", autoClose: 5, font: Program.AlertFont);
                            using (var response = await unityClient.GetAsync(urldownload, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();
                                long totalBytes = response.Content.Headers.ContentLength ?? 0;
                                long downloadedBytes = 0;

                                using (var httpStream = await response.Content.ReadAsStreamAsync())
                                {
                                    using (var fileStream = new FileStream(newFilePath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                                    {
                                        byte[] buffer = new byte[8192];
                                        int bytesRead;

                                        while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                        {
                                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                                            downloadedBytes += bytesRead;

                                            // 更新进度条
                                            if (totalBytes > 0)
                                            {
                                                int progress = (int)((downloadedBytes * 100) / totalBytes);
                                                progress1.Invoke(new Action(() => progress1.Value = progress));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        progress1.Invoke(new Action(() =>
                        {
                            progress1.Visible = false;
                            progress1.Value = 0;
                            progress1.ShowInTaskbar = false;
                        }));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        progress1.Invoke(new Action(() =>
                        {
                            progress1.Visible = false;
                            progress1.Value = 0;
                            progress1.ShowInTaskbar = false;
                        }));
                    }
                    //webClient.DownloadFile(url, newFilePath);
                    AntdUI.Message.success(Program.MainForm, "文件下载完成,准备重启到新版本ヾ(≧▽≦*)o", autoClose: 5, font: Program.AlertFont);
                    Random random = new Random();
                    int randomNum = random.Next(10000, 100000);
                    string oldFileName = $"OldFile{randomNum}.exe";
                    string oldFilePath = Path.Combine(currentDirectory, oldFileName);
                    File.Move(currentFilePath, oldFilePath);
                    Process.Start(new ProcessStartInfo(newFilePath)
                    {
                        UseShellExecute = true
                    });
                    string deleteScriptPath = Path.Combine(currentDirectory, "delete_self.bat");
                    File.WriteAllText(deleteScriptPath, $"@echo off\ncd /d %~dp0\ndel /q \"{oldFileName}\"\ndel /q delete_self.bat");
                    Process.Start(new ProcessStartInfo(deleteScriptPath)
                    {
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    //处理结束后退出程序并执行删除任务
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    AntdUI.Message.open(new AntdUI.Message.Config(Program.MainForm, $"下载失败o(TヘTo)\n原因:{ex.Message}", AntdUI.TType.Error)
                    {
                        AutoClose = 5,
                        Font = Program.AlertFont,
                        ClickClose = true
                    });
                }
            }
        }
        private static async Task<string> FetchUpdateLogAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(urlupdatelog);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return $"Error: {response.StatusCode}||可能是服务正在维护中...";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private void alert2_Click(object sender, EventArgs e)
        {

        }
        //内测名单获取方式
        private bool CheckName()
        {
            string url = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/Preview/List";
            string name = Environment.MachineName;

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                string filePath = Path.Combine(tempPath, "list.txt");

                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, filePath);
                }

                bool contains = false;
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (line.Trim() == name)
                    {
                        contains = true;
                        break;
                    }
                }

                // 只删除下载的文件，不删除文件夹
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                return contains;
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误：" + ex.Message);
                return false;
            }
        }
        private void alert1_Click(object sender, EventArgs e)
        {
            AntdUI.Message.success(Program.MainForm, "已关闭内测接收逻辑", autoClose: 5, font: Program.AlertFont);
        }
        private void label3_Click(object sender, EventArgs e)
        {

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
            var imgPath = ThemeConfig.ReadString("BackPNGAdd");
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
                ApplyColor("SettingButtonColor", c => btn.BackColor = c);
                ApplyColor("SettingButtonColor", c => btn.DefaultBack = c);
            }
        }
    }
}

