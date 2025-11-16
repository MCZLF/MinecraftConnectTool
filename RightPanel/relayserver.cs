using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

namespace MinecraftConnectTool
{
    public partial class relayserver : UserControl
    {
        public relayserver()
        {
            InitializeComponent();
        }
        private TcpPing tcpPing1;
        private TcpPing tcpPing2;
        private TcpPing tcpPing3;

        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void materialLabel2_Click(object sender, EventArgs e)
        {

        }

        private void relayserver_Load(object sender, EventArgs e)
        {
            {
                // 初始化第一个Ping
                tcpPing1 = new TcpPing("101.133.235.149", 56351, 1000);
                tcpPing1.OnPingResult += (ip, latency) => UpdateLabel(materialLabel2, ip, latency);
                tcpPing1.Start();

                // 初始化第二个Ping
                tcpPing2 = new TcpPing("1.12.59.122", 7500, 1000);
                tcpPing2.OnPingResult += (ip, latency) => UpdateLabel(materialLabel1, ip, latency);
                tcpPing2.Start();

                // 初始化第三个Ping
                tcpPing3 = new TcpPing("1.1.1.1", 53, 1000);
                tcpPing3.OnPingResult += (ip, latency) => UpdateLabel(materialLabel3, ip, latency);
                tcpPing3.Start();

                string Server = Form1.config.read<string>("Server", "None");
                if (Server == "MCZLFNEW")
                {
                    materialCheckBox1.Checked = true;
                }
                else if (Server == "master40")
                {
                    materialCheckBox2.Checked = true;
                }
            }
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

        private void button2_Click(object sender, EventArgs e)
        {
            materialCheckBox2.Checked = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            materialCheckBox1.Checked = true;
 //           AntdUI.Message.info(Program.MainForm, $"点那个半个勾勾,这个按钮暂时还点不了", autoClose: 5, font: Program.AlertFont);
        }

        private void materialCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (materialCheckBox1.Checked)
            {
                if (materialCheckBox2.Checked)
                { materialCheckBox2.Checked = false; }
                Form1.config.write("Server", "MCZLFNEW");
                Form1.config.write("EnableRelay", true);
                materialCheckBox1.Checked = true;
                AntdUI.Message.info(Program.MainForm, $"设置将在下一次启动时生效", autoClose: 5, font: Program.AlertFont);
            }
            else
            {
                Form1.config.delete("Server");
                Form1.config.write("EnableRelay", false);
                materialCheckBox1.Checked = false;
            }
        }

        private void materialCheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (materialCheckBox2.Checked)
            {
                if (materialCheckBox1.Checked)
                { materialCheckBox1.Checked = false; }
                Form1.config.write("Server", "master40");
                Form1.config.write("EnableRelay", true);
                materialCheckBox2.Checked = true;
                AntdUI.Message.info(Program.MainForm, $"设置将在下一次启动时生效", autoClose: 5, font: Program.AlertFont);
            }
            else
            {
                Form1.config.delete("Server");
                Form1.config.write("EnableRelay", false);
                materialCheckBox2.Checked = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            throw new Exception("LEMON爆炸了");
        }
    }
}
