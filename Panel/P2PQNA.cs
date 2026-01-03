using AntdUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class P2PQNA : UserControl
    {
        public P2PQNA()
        {
            InitializeComponent();
            color();
        }
        public static Font P2PFont { get; } = new Font("Microsoft YaHei UI", 9f);
        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void alert1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {
            string textToCopy = "https://www.mcmod.cn/class/11743.html\n" +
                                "https://www.mcmod.cn/class/1875.html\n" +
                                "https://www.mcmod.cn/class/12625.html\n" +
                                "https://www.mcmod.cn/class/16197.html";

            Clipboard.SetText(textToCopy);
            AntdUI.Message.warn(Program.MainForm, "成功复制到剪切板！", autoClose: 5, font: P2PFont);

        }

        private void alert7_Click(object sender, EventArgs e)
        {
            string url = "https://qm.qq.com/q/V6SQQIA8YU";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开网址: {url}\n错误信息: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void alert8_Click(object sender, EventArgs e)
        {
            string url = "https://mct.mczlf.loft.games/";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开网址: {url}\n错误信息: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void collapse1_ExpandChanged(object sender, AntdUI.CollapseExpandEventArgs e)
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
            ApplyColor("P2PBack", c => this.BackColor = c);
        }
    }
}
