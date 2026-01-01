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
    public partial class InviteinfoEdit : UserControl
    {
        public InviteinfoEdit()
        {
            InitializeComponent();
            InitializeDropdown();
        }

        private void infoEdit_Load(object sender, EventArgs e)
        {
            int updatecode = Form1.config.read<int>("codeupdate", 1);
            if (updatecode == 1)
            {
                dropdown1.Text = "每次";
                badge1.State = TState.Success;
            }
            else if (updatecode == 2)
            {
                dropdown1.Text = "每时";
                badge1.State = TState.Success;
            }
            else if (updatecode == 3)
            {
                dropdown1.Text = "每天";
                badge1.State = TState.Success;
            }
            else if (updatecode == 4)
            {
                dropdown1.Text = "永久";
                badge1.State = TState.Success;
                codeshow_Forever();
            }
        }
        private void InitializeDropdown()
        {
            int maxConnections = Form1.config.read<int>("Bar", 1);
            dropdown1.Items.Clear();
            dropdown1.Items.AddRange(new AntdUI.SelectItem[]
            {
                new AntdUI.SelectItem("每次"), // 默认选中项
                new AntdUI.SelectItem("每时"),
                new AntdUI.SelectItem("每天"),
                new AntdUI.SelectItem("永久"),
            });
            dropdown1.SelectedValueChanged += Dropdown1_SelectedValueChanged;
        }
        private void Dropdown1_SelectedValueChanged(object sender, AntdUI.ObjectNEventArgs e)
        {
            var selectedValue = e.Value.ToString();
            switch (selectedValue)
            {
                case "每次":
                    Form1.config.write("codeupdate", 1);
                    dropdown1.Text = "每次";
                    hiddenButton_Froever();
                    badge1.State = TState.Success;
                    AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: Program.AlertFont);
                    break;
                case "每时":
                    Form1.config.write("codeupdate", 2);
                    dropdown1.Text = "每时";
                    hiddenButton_Froever();
                    badge1.State = TState.Success;
                    AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: Program.AlertFont);
                    break; 
                case "每天":
                    Form1.config.write("codeupdate", 3);
                    dropdown1.Text = "每天";
                    hiddenButton_Froever();
                    badge1.State = TState.Success;
                    AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: Program.AlertFont);
                    break;
                case "永久":
                    Form1.config.write("codeupdate", 4);
                    dropdown1.Text = "永久";
                    badge1.State = TState.Default;
                    AntdUI.Message.info(Program.MainForm, $"设置将在重启后生效", autoClose: 5, font: Program.AlertFont);
                    codeshow_Forever();
                    break;
                default:
                    // 如果选中的是默认项或其他项，不做处理
                    break;
            }
        }
        private void codeshow_Forever()
        {
            savebut2.Loading = false;
            //展示提示框
            input2.Visible = true;
            savebut2.Visible = true;
            bool usecustomnode = Form1.config.read<bool>("usecustomnode", false);
            if (usecustomnode)
            {
                string customnode = Form1.config.read<string>("customnode", "不存在");
                input2.Text = customnode;
                input2.Visible = true;
            }
        }
        private void hiddenButton_Froever()
        { input2.Visible = false;  savebut2.Visible = false; }
        private void input2_TextChanged(object sender, EventArgs e)
        {
            string customportc = Form1.config.read<string>("customnode", "不存在");
            if (customportc == input2.Text)
            {
                savebut2.Visible = false;
            }
            else
            {
                Form1.config.delete("customnode");
                savebut2.Loading = false;
                savebut2.Visible = true;
            }
        }

        private async void savebut2_Click(object sender, EventArgs e)
        {
            savebut2.Loading = true;
            if (System.Text.RegularExpressions.Regex.IsMatch(input2.Text, @"[^\u4e00-\u9fa5a-zA-Z0-9\-]"))
            {
                AntdUI.Message.error(Program.MainForm, "提示码仅可输入汉字、数字、字母或-连字符", autoClose: 5, font: Program.AlertFont);
                //savebut2.Visible = false;
                savebut2.Loading = false;
                return;
            }
            // 敏感词检测
            if (dropdown1.Text=="永久" && input2.Text.Length >= 8)
            {
                try
                {
                    var o = Newtonsoft.Json.Linq.JObject.Parse(
                        await new System.Net.Http.HttpClient().GetStringAsync(
                            $"https://uapis.cn/api/prohibited?text={System.Uri.EscapeDataString(input2.Text)}"));
                    if ((string)o["status"] == "forbidden")
                    {
                        AntdUI.Message.error(Program.MainForm,
                            $"包含敏感词：{(string)o["forbiddenWord"]}", autoClose: 5, font: Program.AlertFont);
                        return;
                    }
                }
                catch
                {
                    AntdUI.Message.error(Program.MainForm, $"网络异常", autoClose: 5, font: Program.AlertFont); return;
                }

                Form1.config.write("usecustomnode", true);
                Form1.config.write("customnode", input2.Text);
            }
            else if (dropdown1.Text == "永久")
            {
                AntdUI.Message.error(Program.MainForm, "提示码过段,需要至少8位", autoClose: 5, font: Program.AlertFont);
                Form1.config.write("usecustomnode", false);
            }
            else
            {
                Form1.config.delete("customnode");
                Form1.config.write("usecustomnode", false);
            }

            savebut2.Visible = false;
        }

        private void dropdown1_SelectedValueChanged_1(object sender, ObjectNEventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"Info", "自定义加入方的端口\n开启后加入ip将在下一次启动时更新为 127.0.0.1:自定义端口", AntdUI.TType.Info)
            {
                CloseIcon = true,
                Font = Program.AlertFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }

        private void switch1_CheckedChanged(object sender, BoolEventArgs e)
        {
            savebut.Loading = false;
            if (switch1.Checked)
            {
                input1.Visible = true;
                switch1.Checked = true;
            }
            else
            {
                savebut.Visible = true;
                input1.Visible = false;
                switch1.Checked = false;
            }
        }

        private void input1_TextChanged(object sender, EventArgs e)
        {
            string customportc = Form1.config.read<string>("customport", "不存在");
            if (customportc == input1.Text)
            {
                savebut.Visible = false;
            }
            else
            {
                Form1.config.delete("customport");
                savebut.Loading = false;
                savebut.Visible = true;
            }
        }

        private void savebut_Click(object sender, EventArgs e)
        {
            savebut.Loading = true;
            string customport = input1.Text;
            if (switch1.Checked == true)
            {
                if (int.TryParse(customport, out int port) && port >= 1 && port <= 65535)
                {
                    Form1.config.write("usecustomport", true);
                    Form1.config.write("customport", customport);
                }
                else
                {
                    AntdUI.Message.error(Program.MainForm, "请输入有效的端口(1-65535)", autoClose: 5, font: Program.AlertFont);
                    Form1.config.write("usecustomport", false);
                }
            }
            if (switch1.Checked == false)
            {
                Form1.config.delete("customport");
                Form1.config.write("usecustomport", false);
            }
            savebut.Visible = false;
        }

    }
}
