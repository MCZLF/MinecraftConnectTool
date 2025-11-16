using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class AdvancedSettings : UserControl
    {
        public AdvancedSettings()
        {
            InitializeComponent();
        }

        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void materialRadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (materialRadioButton1.Checked)
            {
                if (materialRadioButton2.Checked)
                { materialRadioButton2.Checked = false; }
                Form1.config.write("TCP", true);
                materialRadioButton1.Checked = true;
                label1.Visible = false; ;
            }
            else
            {
                Form1.config.write("TCP", false);  //我懒了，true = TCP false = UDP
                materialRadioButton1.Checked = false;
                label1.Visible = true;
            }
        }

        private void materialRadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (materialRadioButton2.Checked)
            {
                if (materialRadioButton1.Checked)
                { materialRadioButton1.Checked = false; }
                Form1.config.write("TCP", false);
                materialRadioButton2.Checked = true;
                label1.Visible = true;
                bool userelay = Form1.config.read<bool>("EnableRelay", false);
                if (userelay)
                {
                    Form1.config.delete("Server");
                    Form1.config.write("EnableRelay", false);
                    Program.alertwarn("自定义Relay中转与UDP协议暂时不兼容\n自定义中转已被关闭并删除");
                }
                //IF其他兼容选项,也一并关闭
                bool EnableOLAN = Form1.config.read<bool>("EnableOLAN", false);
                if (EnableOLAN) { Form1.config.write("EnableOLAN", false); Program.alertwarn("OneLauncher兼容与UDP协议暂时不兼容\n该选项已被设置为关闭"); }
                bool EnableTL = Form1.config.read<bool>("EnableTL", false);
                if (EnableTL) { Form1.config.write("EnableTL", false); Program.alertwarn("泰拉瑞亚(TCP)兼容与UDP协议暂时不兼容\n该选项已被设置为关闭"); }
                bool EnableDST = Form1.config.read<bool>("EnableDST", false);
                if (EnableDST) { Form1.config.write("EnableDST", false); Program.alertwarn("饥荒联机版与UDP协议暂时不兼容\n该选项已被设置为关闭"); }
                bool ServerPostEnable = Form1.config.read<bool>("ServerPostEnable", false);
                if (ServerPostEnable)
                {AntdUI.Message.info(Program.MainForm, "房间多播|该功能可能无法在UDP模式下正常工作", autoClose: 5, font: Program.AlertFont);   }}
            else
            {
                Form1.config.write("TCP", true);  //我懒了，true = TCP false = UDP
                materialRadioButton2.Checked = false;
                label1.Visible = false;
            }
        }

        private void AdvancedSettings_Load(object sender, EventArgs e)
        {
            bool TCP = Form1.config.read<bool>("TCP", true);
            if (TCP)
            {
                materialRadioButton1.Checked = true;
            }
            else { materialRadioButton2.Checked = true;label1.Visible = true; }
            bool Testchannel = Form1.config.read<bool>("Testchannel", false);
            if (Testchannel)
            {
                switch2.Checked = true;
            }
            switch1.Checked = !File.Exists(Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "Probe"));
        }

        private void button4_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", "启用后将会将切换至测试通道\n该通道与0.0.6.091(SP1测试版本)前所有版本都不兼容\n若使用不同的通道则会提示对方不在线\n由于测试需要,当前通道暂时只允许最低每天刷新提示码\n开启后将会自动切换至每天更新(如为永久则不会切换)", AntdUI.TType.Info)
            {
                CloseIcon = true,
                Font = Program.AlertFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }

        private void switch2_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
        {
            if (switch2.Checked)
            {
                Form1.config.write("Testchannel", true);
                switch2.Checked = true;
                int codeupdate = Form1.config.read<int>("codeupdate", 1);
                if (codeupdate != 3 && codeupdate != 4)
                {
                    Form1.config.write("codeupdate", 3);
                    AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", "已将提示码更新频率设置为每天)", AntdUI.TType.Info)
                    {
                        CloseIcon = true,
                        Font = Program.AlertFont,
                        Draggable = false,
                        CancelText = null,
                        OkText = "好的"
                    });
                }
                AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: Program.AlertFont);
            }
            else
            {
                Form1.config.write("Testchannel", false);
                switch2.Checked = false; 
                AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: Program.AlertFont);
            }
        }

        private void switch1_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
        {
            var flag = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "Probe");

            try
            {
                if (switch1.Checked)          // 打开 → 删标记
                {
                    if (File.Exists(flag))
                        File.Delete(flag);
                }
                else                          // 关闭 → 写标记
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(flag));
                    File.WriteAllText(flag, "这真的很让人难过,如果可以我们还是建议开启该选项,删除该文件即可直接重新开启 [设置>高级设置>允许Probe探针]");
                }
            }
            catch
            {
                // 静默忽略
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", "这个选项的作用类似于 [告诉开发者我正在使用]\n鉴于PCL PCLCE HMCL均已携EasyTier方案重启联机\nMCT需要统计实际使用数据避免无用消耗\n\n当然了,这并不意味着终止更新,因为服务器续费了很久很久...\n(真的很久)   如果可以,保持开启以示支持,可以吗(小声)", AntdUI.TType.Info)
                {
                    CloseIcon = true,
                    Font = Program.AlertFont,
                    Draggable = false,
                    CancelText = null,
                    OkText = "好的"
                });
            }
    }
}
}
