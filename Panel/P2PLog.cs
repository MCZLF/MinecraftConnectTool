using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class P2PLog : UserControl
    {
        public P2PLog()
        {
            InitializeComponent();
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;
        }

        private void P2PLog_Load(object sender, EventArgs e)
        {
            color();
            LoadEntireFileToRichTextBox();
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
                ApplyColor("P2PBack", c => button1.BackColor = c);
                ApplyColor("P2PBack", c => button1.DefaultBack = c);
                ApplyColor("P2PBack", c => button2.BackColor = c);
                ApplyColor("P2PBack", c => button2.DefaultBack = c);
                ApplyColor("P2PBack", c => button3.BackColor = c);
                ApplyColor("P2PBack", c => button3.DefaultBack = c);
                ApplyColor("LogPanelBack", c => richTextBox1.BackColor = c);
            }
        }
        private void LoadEntireFileToRichTextBox()
        {
            string logFilePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "MCZLFAPP", "Temp", "APPLog.ini");
            if (!File.Exists(logFilePath))
            {
                MessageBox.Show("日志文件不存在。", "文件缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            FileInfo fileInfo = new FileInfo(logFilePath);
            if (fileInfo.Length > 20 * 1024 * 1024) // 20MB
            {
                MessageBox.Show("日志文件异常,无法加载。", "文件异常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string fileContent = File.ReadAllText(logFilePath);
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(() =>
                {
//                    richTextBox1.Clear();
                    richTextBox1.AppendText(fileContent);
                    richTextBox1.ScrollToCaret();
                }));
            }
            else
            {
//                richTextBox1.Clear();
                richTextBox1.AppendText(fileContent);
                richTextBox1.ScrollToCaret();
            }
        }
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            LoadEntireFileToRichTextBox();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadFileOpenp2plog();
        }

        private void LoadFileOpenp2plog()
        {
            // 定义新的日志文件路径
            string logFilePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "MCZLFAPP", "Temp", "log", "openp2p.log");

            // 检查文件是否存在
            if (!File.Exists(logFilePath))
            {
                MessageBox.Show("openp2p.log 文件不存在。", "文件缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 获取文件信息
            FileInfo fileInfo = new FileInfo(logFilePath);

            // 检查文件大小是否超过20MB
            if (fileInfo.Length > 20 * 1024 * 1024) // 20MB
            {
                MessageBox.Show("openp2p.log 文件异常，无法加载。", "文件异常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 使用后台线程读取文件内容
            Task.Run(() =>
            {
                // 读取文件内容
                string fileContent = File.ReadAllText(logFilePath);

                // 在主线程中更新 RichTextBox 的内容
                if (richTextBox1.InvokeRequired)
                {
                    richTextBox1.Invoke(new Action(() =>
                    {
                        richTextBox1.Clear();
                        richTextBox1.AppendText(fileContent);
                        richTextBox1.ScrollToCaret();
                    }));
                }
                else
                {
                    // 如果需要清空旧内容，可以取消注释以下代码
                    // richTextBox1.Clear();
                    richTextBox1.AppendText(fileContent);
                    richTextBox1.ScrollToCaret();
                }
            });
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string defaultFileName = $"log_{timestamp}.log";

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "MCT日志文件(*.log)|*.log",
                FileName = defaultFileName,
                Title = "保存MCT日志文件"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, richTextBox1.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            System.Diagnostics.Process.Start(Path.GetDirectoryName(saveFileDialog.FileName));
        }
    }
}
