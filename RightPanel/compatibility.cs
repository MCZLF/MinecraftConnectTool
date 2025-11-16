using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;

namespace MinecraftConnectTool
{
    public partial class compatibility : UserControl
    {
        public compatibility()
        {
            InitializeComponent();
            bool EnableOLAN = Form1.config.read<bool>("EnableOLAN", false);
            if (EnableOLAN) { oneluancherswitch.Checked = true; }
            bool EnableTL = Form1.config.read<bool>("EnableTL", false);
            if (EnableTL) { materialCheckBox1.Checked = true; }
            bool EnableDST = Form1.config.read<bool>("EnableDST", false);
            if (EnableDST) { materialCheckBox3.Checked = true; }
        }

        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void oneluancherswitch_CheckedChanged(object sender, EventArgs e)
        {
            if (oneluancherswitch.Checked)
            {
                if (materialCheckBox1.Checked)
                { materialCheckBox1.Checked = false; }
                Form1.config.write("EnableOLAN", true);
                oneluancherswitch.Checked = true;
                bool TCP = Form1.config.read<bool>("TCP", true);
                if (!TCP) { Form1.config.write("TCP", true); Program.alertwarn("全局UDP模式与当前选项不兼容\n运行模式已修改为TCP模式"); }
            }
            else
            {
                Form1.config.write("EnableOLAN", false);
                oneluancherswitch.Checked = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/abbcccbba/OneLauncher/");
        }

        private void materialCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (materialCheckBox1.Checked)
            {
                if (oneluancherswitch.Checked)
                { oneluancherswitch.Checked = false; }
                if (materialCheckBox3.Checked)
                { materialCheckBox3.Checked = false; }
                Form1.config.write("EnableTL", true);
                materialCheckBox1.Checked = true;
                AntdUI.Message.info(Program.MainForm, "泰拉瑞亚的默认联机端口是7777,如果有手动修改请以实际为准\n并在加入时重新写入正确端口", autoClose: 5, font: Program.AlertFont);
                bool TCP = Form1.config.read<bool>("TCP", true);
                if (!TCP) { Form1.config.write("TCP", true); Program.alertwarn("全局UDP模式与当前选项不兼容\n运行模式已修改为TCP模式"); }
            }
            else
            {
                Form1.config.write("EnableTL", false);
                materialCheckBox1.Checked = false; 
            }
        }

        private void compatibility_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.terraria.org/");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://store.steampowered.com/app/322330/dont_starve_together/");
        }

        private void materialCheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (materialCheckBox3.Checked)
            {
                if (oneluancherswitch.Checked)
                { oneluancherswitch.Checked = false; }
                if(materialCheckBox1.Checked)
                { materialCheckBox1.Checked = false; }
                Form1.config.write("EnableDST", true);
                materialCheckBox3.Checked = true;
                bool TCP = Form1.config.read<bool>("TCP", true);
                if (!TCP) { Form1.config.write("TCP", true); Program.alertwarn("全局UDP模式与当前选项不兼容\n运行模式已修改DST兼容模式"); }}
            else
            {
                Form1.config.write("EnableDST", false);
                materialCheckBox3.Checked = false;
            }
        }
    }
}
