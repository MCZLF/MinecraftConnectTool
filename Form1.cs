using AntdUI;
using MaterialSkin.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class Form1 : AntdUI.Window
    {
        public Form1()
        {
            InitializeComponent();
            menu1.SelectChanged += Menu1_SelectChanged;
            this.Shown += new EventHandler(Form1_Shown);
            this.FormClosing += MainForm_FormClosing;
            AntdUI.Config.ShowInWindow = true;
        }
        public static readonly string version = "0.0.6.103Test";
        public static readonly string designation = "我们生而眺望_摘自 鸣潮3.0";
        //地址github.com/MCZLF/MinecraftConnectTool 
        //Version放开头的传统不能变
        public static readonly bool dark = false; //临时开关
        public static readonly bool ISNMode = false;
        public static readonly bool EnableReceiveNMode = false;
        public static Dictionary<Type, Control> GlobalCache = new Dictionary<Type, Control>(); //缓存字典
        private readonly List<object> _floatButtons = new List<object>(); //启动时监测进程的字典
        //各项子功能控制放开头的传统也不能变
        public static Font formfont { get; } = new Font("Microsoft YaHei UI", 9f);        //form1中的全局字体 714后的新字体统一使用Program中的public
        private void Form1_Shown(object sender, EventArgs e)
        {
            Probe.EnablePopup = false;
            //Program.alertwarn(this.CurrentAutoScaleDimensions.ToString());
            if (Process.GetProcessesByName("main").Length > 0)
            {
                ShowButton();
                Debug.Print("核心运行中");
            }
            bool nonotifywhenstart = Form1.config.read<bool>("nonotifywhenstart", false);
            if (nonotifywhenstart) { }
            else
            {
                AntdUI.Notification.info((Form)this, "info", "欢迎使用Minecraft Connect Tool~", align: AntdUI.TAlignFrom.BR, font: formfont);
                //AntdUI.Notification.success((Form)this, "info", "成功的加载了ToastNotification3", align: AntdUI.TAlignFrom.BR, font: formfont);
            }
            if (version.Contains("测试版本"))
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info\n这是一个测试版本 版本号为{version}\n该提醒不可关闭,每次启动时强制提醒", "出现bug欢迎反馈,可在群里反馈690625244\n或是向admin@mczlf.xyz发送邮件,标题备注[BUG反馈]", AntdUI.TType.Info)
                {
                    CloseIcon = true,
                    Font = Program.AlertFont,
                    Draggable = false,
                    CancelText = null,
                    OkText = "好的"
                });
            }
            if (ISNMode)
            {
                bool contains = CheckName();
                if (contains)
                {
                    if (version.Contains("测试版本"))
                    {
                        AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Warn\n这是一个内测版本 版本号为{version}\n该提醒不可关闭,每次启动时强制提醒", "出现bug欢迎反馈,私信我,可以截图发群里也行", AntdUI.TType.Warn)
                        {
                            CloseIcon = true,
                            MaskClosable = false,
                            Keyboard = false,
                            Font = Program.AlertFont,
                            Draggable = false,
                            CancelText = null,
                            OkText = "好的"
                        });
                    }
                }
                else
                {
                    string node = System.Environment.MachineName;
                    AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"ERROR\n该版本无法在当前环境下工作\nNode:{node}", $"关闭窗口后自动退出\nVersion:{version}", AntdUI.TType.Error)
                    {
                        CloseIcon = true,
                        MaskClosable = false,
                        Keyboard = false,
                        Font = Program.AlertFont,
                        Draggable = false,
                        CancelText = null,
                        OkText = "好的"
                    });
                    Application.Exit();
                }
            }
            if (!File.Exists(Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "Probe")))
                _ = Probe.SendAsync();
            bool EnableVersionCheck = Form1.config.read<bool>("EnableVersionCheck", true);
            if (EnableVersionCheck)
            {
                Task.Run(() => supportcheck.Check());
            }
            try { color(); }
            catch (Exception ex)
            {
                MessageBox.Show($"主题加载失败：\n{ex}",
                                "启动错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            bool EnableATDDark = Form1.config.read<bool>("EnableATDDark", false);
            if (EnableATDDark)
            {
                AntdUI.Config.IsDark = true; }
            }

        private void Menu1_SelectChanged(object sender, MenuSelectEventArgs e)
        {
            string tag;
            try
            {
                tag = e.Value.Tag.ToString();
            }
            catch (Exception ex)
            {
                tag = "default";
                MessageBox.Show($"哎呀，貌似出了点小问题\n{ex}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            switch (tag)
            {
                case "P2PHome":
                    MessageBox.Show("[调试信息]重复校验1", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "P2PControl":
                    LoadPanel(panel, typeof(P2PMode));
                    break;
                case "P2PLog":
                    LoadPanel(panel, typeof(P2PLog));
                    break;
                case "ZTHome":
                    LoadPanel(panel, typeof(LinkMode));
                    AntdUI.Message.warn(Program.MainForm, $"该未成形联机方式将在未来版本更新为其他联机方式,现不推荐使用", autoClose: 5, font: formfont);
                    break;
                case "Update":
                    LoadPanel(panel, typeof(Update));
                    break;
                case "Settings":
                    LoadPanel(panel, typeof(Settings));
                    break;
                case "P2PQA":
                    LoadPanel(panel, typeof(P2PQNA));
                    break;
                default:
                    //MessageBox.Show("点击了其他菜单项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
        }

        public void LoadPanel(AntdUI.Panel panel, Type controlType)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            if (controlType == null) throw new ArgumentNullException(nameof(controlType));

            // 仅 P2PMode 和 Settings 走缓存
            if (controlType == typeof(MinecraftConnectTool.P2PMode) || controlType == typeof(MinecraftConnectTool.Settings))
            {
                if (!Form1.GlobalCache.TryGetValue(controlType, out var cached))
                {
                    cached = (Control)Activator.CreateInstance(controlType);
                    Form1.GlobalCache[controlType] = cached;
                }
                panel.Controls.Clear();
                panel.Controls.Add(cached);
                cached.Dock = DockStyle.Fill;
                return;
            }

            // 其他类型每次都重新创建
            panel.Controls.Clear();
            var instance = (Control)Activator.CreateInstance(controlType);
            if (instance == null)
                throw new InvalidOperationException($"Failed to create control of type {controlType.Name}");

            panel.Controls.Add(instance);
            instance.Dock = DockStyle.Fill;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            PageHeader.SubText = version;
            if (Program.admin == 2)
            { Console.WriteLine("非管理员启动"); }
            else { Console.WriteLine("管理员模式"); }
            ;
            bool goupd = config.read<bool>("goupdatewhenstart");
            int bar = config.read<int>("Bar", 1);
            if (goupd)
            {
                LoadPanel(panel, typeof(Update));
            }
            else
            { LoadPanel(panel, typeof(P2PMode)); }
            if (bar == 1)
            {
                LoadPanel(panelbar, typeof(lanload));
            }
            else if (bar == 2)
            {
                LoadPanel(panelbar, typeof(wheatherbar));
            }
            else if (bar == 3)
            {
                MessageBox.Show("没有，不知道做什么");
            }
            else
            {
                AntdUI.Message.error(Program.MainForm, $"[调试信息]Bar = 0，Bar不显示", autoClose: 5, font: formfont);
            }
            //显示Greeting
            string userName = Environment.UserName;
            string greeting = GetGreetingMessage(userName);
            AntdUI.Message.open(new AntdUI.Message.Config(this, (greeting), AntdUI.TType.Info)
            {
                Font = new Font("Microsoft YaHei UI", 11.2f),
                AutoClose = 3,
                ClickClose = true,
                Align = TAlignFrom.Bottom,
                Radius = 25
            });
        }
        public void ShowButton()
        {
            CreateFloatButton(new FloatButton.Config(this, new[]{
                new FloatButton.ConfigBtn("CloseButton", Properties.Resources.close){
                Text = "关闭",
                Tooltip = "关闭P2P核心-all(Form)",
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
                Font = Program.AlertFont
            });
        }
        public void stopp2p()
        {
            Server_Post.Stop_Post();
            ClearAllFloatButtons();
            Process[] processes = Process.GetProcessesByName("main");
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill(); // 强制结束进程
                    process.WaitForExit(); // 等待进程完全退出
                    AntdUI.Message.info(Program.MainForm, $"已结束进程", autoClose: 5, font: formfont);
                }
                catch (Exception ex)
                {
                    AntdUI.Message.error(Program.MainForm, $"无法关闭进程: {ex.Message}", autoClose: 15, font: formfont);
                }
            }

            if (processes.Length == 0)
            {
                AntdUI.Message.error(Program.MainForm, "未找到进程", autoClose: 15, font: formfont);
            }
        }
        /// <summary>
        /// 创建 FloatButton 并记录引用
        /// </summary>
        private void CreateFloatButton(FloatButton.Config cfg)
        {
            var btn = FloatButton.open(cfg);   // object 类型
            _floatButtons.Add(btn);
        }
        /// <summary>
        /// 关闭并清空所有已创建的 FloatButton
        /// </summary>
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


        private void PageHeader_Click(object sender, EventArgs e)
        {

        }

        private void panel_Click(object sender, EventArgs e)
        {

        }

        private void menu1_SelectChanged_1(object sender, MenuSelectEventArgs e)
        {

        }
        private string GetGreetingMessage(string userName)
        {
            int hour = DateTime.Now.Hour;
            if (hour >= 6 && hour < 12)
            {
                return $"上午好,{userName}";
            }
            else if (hour >= 12 && hour < 14)
            {
                return $"中午好,{userName}";
            }
            else if (hour >= 14 && hour < 18)
            {
                return $"下午好,{userName}";
            }
            else
            {
                return $"晚上好,{userName}";
            }
        }

        private void buttonSZ_Click(object sender, EventArgs e)
        {
            LoadPanel(panel, typeof(Settings));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Server_Post.Stop_Post();
            config.delete("Server");
            config.write("EnableRelay", false);
        }

        //config
        public static class config
        {
            private static string ConfigFilePath => Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "MCZLFAPP", "Temp", "APPconfig.json");

            // 初始化配置文件
            static config()
            {
                // 确保目录存在
                var configDir = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                if (!File.Exists(ConfigFilePath))
                {
                    var defaultConfig = new JObject
                    {
                        ["goupdatewhenstart"] = false,
                        ["Bar"] = 1,
                        ["AutoCheckP2PIFOpen"] = true,
                        ["ServerPostEnable"] = true
                    };

                    File.WriteAllText(ConfigFilePath, defaultConfig.ToString(Formatting.Indented));
                }
            }

            // 读取配置项
            public static T read<T>(string key, T defaultValue = default)
            {
                try
                {
                    var config = JObject.Parse(File.ReadAllText(ConfigFilePath));
                    if (config[key] != null)
                    {
                        return config[key].ToObject<T>();
                    }
                }
                catch (Exception ex)
                {
                    // 如果读取失败或键不存在，记录异常并返回默认值
                    Console.WriteLine($"Error reading config key '{key}': {ex.Message}");
                }
                return defaultValue;
            }

            // 写入配置项
            public static void write(string key, object value)
            {
                try
                {
                    var config = File.Exists(ConfigFilePath)
                        ? JObject.Parse(File.ReadAllText(ConfigFilePath))
                        : new JObject();

                    config[key] = JToken.FromObject(value);

                    File.WriteAllText(ConfigFilePath, config.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing config key '{key}': {ex.Message}");
                }
            }

            //删除配置项
            public static void delete(string key)
            {
                try
                {
                    var config = File.Exists(ConfigFilePath)
                        ? JObject.Parse(File.ReadAllText(ConfigFilePath))
                        : new JObject();
                    if (config[key] != null)
                    {
                        config.Remove(key);
                        File.WriteAllText(ConfigFilePath, config.ToString(Formatting.Indented));
                    }
                    else
                    {
                        Console.WriteLine($"Key '{key}' not found in config file.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting config key '{key}': {ex.Message}");
                }
            }
        }

        private void button_color_Click(object sender, EventArgs e)
        {
            LoadPanel(panel, typeof(Help));
        }
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

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var type = panel.Controls[0].GetType();
                panel.Controls[0].Dispose();
                panel.Controls.Clear();
                var newCtl = (Control)Activator.CreateInstance(type);
                Form1.GlobalCache[type] = newCtl;

                panel.Controls.Add(newCtl);
                newCtl.Dock = DockStyle.Fill;
            }
            catch (Exception ex)
            {
                throw new Exception($"重载失败：{ex.Message}", ex);
            }
        }

        private void input_search_TextChanged(object sender, EventArgs e)
        {

        }


        private async void button_mclogs_Click(object sender, EventArgs e)
        {
            button_mclogs.Loading = true;
            const string api = "https://api.mclo.gs/1/log";
            try
            {
                string logPath = Path.Combine(
                    Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                    "MCZLFAPP", "Temp", "APPLog.ini");
                string configPath = Path.Combine(
                    Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                    "MCZLFAPP", "Temp", "config.json");
                string AppconfigPath = Path.Combine(
                    Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                    "MCZLFAPP", "Temp", "APPconfig.json");

                //GetNetVersion
                string Netversion = System.Environment.Version.ToString();
                //GetSysVersion
                string SystemEvVersion = System.Environment.OSVersion.VersionString + " " + (System.Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");

                if (!File.Exists(logPath) || !File.Exists(configPath) || !File.Exists(AppconfigPath))
                {
                    MessageBox.Show("日志文件不存在！", "云日志上传",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    button_mclogs.Loading = false;
                    return;
                }

                string logContent = File.ReadAllText(logPath, Encoding.UTF8);
                string logAppConfig = File.ReadAllText(AppconfigPath, Encoding.UTF8);
                string logConfig = File.ReadAllText(configPath, Encoding.UTF8);
                var j = JObject.Parse(logConfig);
                var app = j["apps"]?.FirstOrDefault() as JObject;
                logConfig = JsonConvert.SerializeObject(new
                {
                    Node = (string)j["network"]?["Node"],
                    SrcPort = app?["SrcPort"],
                    PeerNode = app?["PeerNode"],
                    DstPort = app?["DstPort"],
                    RelayNode = app?["RelayNode"],
                    Enabled = app?["Enabled"],
                    App = app ?? null
                }, Formatting.Indented);
                string body =
        $@"UploadTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
MachineName: {Environment.MachineName}
APPVersion: {Form1.version}
NetVersion:{Netversion}
SysVersion:{SystemEvVersion}

=========P2PConfig==============
{logConfig}

========APPConfig.ini===========
{logAppConfig}

===Full_APPLog.ini===
{logContent}";
                body = body
                       .Replace("tokenNormal", "MinecraftConnectTool")
                       .Replace("16947733", "690625244")
                       .Replace("openp2p.cn@gmail.com", "admin@mczlf.xyz")
                       .Replace("openp2p start", "Powered by OpenP2P");
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
                    MessageBox.Show($"上传成功！\n日志地址：{url}\n已复制入剪切板中,可直接粘贴哦", "云日志上传", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    button_mclogs.Loading = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"上传失败：{ex.Message}", "云日志上传", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button_mclogs.Loading = false;
            }
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
            ApplyColor("Title", c => PageHeader.DividerColor = c);
            ApplyColor("Title", c => PageHeader.BackColor = c);
            ApplyColor("LeftMenu", c => input_search.BackColor = c);
            ApplyColor("LeftMenu", c => menu1.BackColor = c);
            ApplyColor("LeftMenuHover", c => menu1.BackHover = c);
            }
        }
    }
}