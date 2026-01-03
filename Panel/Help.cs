using AntdUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MinecraftConnectTool
{
    public partial class Help : UserControl
    {
        public Help()
        {
            InitializeComponent();
        }

        private void FirstStart_Load(object sender, EventArgs e)
        {
            string userName = Environment.UserName;
            string greeting = GetGreetingMessage(userName);
            label1.Text = $"Hi,{greeting}";
            string tempPath = Environment.GetEnvironmentVariable("TEMP");
            string targetPath = Path.Combine(tempPath, "MCZLFAPP", "Temp");
            if (!Directory.Exists(targetPath))
            {Directory.CreateDirectory(targetPath);}
            color();
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

        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Preview.open(new Preview.Config(Program.MainForm, new List<Image>() { Properties.Resources.fangzhu, Properties.Resources.wanjia }));
        }

        private void alert1_Click(object sender, EventArgs e)
        {
            Process.Start("http://mcjavao.tttttttttt.top/");
        }

        private void alert2_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.bilibili.com/video/BV1sBXyYgE1j/");
        }

        private void alert3_Click(object sender, EventArgs e)
        {
            Process.Start("https://qm.qq.com/q/rEpogkKhry");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("https://qm.qq.com/q/8NAoszhKqk");
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            //测试下新UI元素 加入日期 2025 07 10 FF版本
            var customModal = new InteractiveModal();
            string backInfo = customModal.ShowModal(Program.MainForm, "请输入柠檬寂寞大核弹");
            MessageBox.Show("返回的输入内容为：" + backInfo); // 显示返回的输入内容
        }
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
                    ApplyColor("P2PButton", c => btn.BackColor = c);
                    ApplyColor("P2PButton", c => btn.DefaultBack = c);
                }
            }
        }

    }
}