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
using AntdUI;

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
    }
}
