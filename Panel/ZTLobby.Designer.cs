namespace MinecraftConnectTool
{
    partial class ZTLobby
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.create = new AntdUI.Button();
            this.networkname = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.pass = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.dhcp = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.roomname = new AntdUI.Input();
            this.roompwd = new AntdUI.Input();
            this.TopText = new System.Windows.Forms.RichTextBox();
            this.progress1 = new AntdUI.Progress();
            this.roompwdjoin = new AntdUI.Input();
            this.roomnamejoin = new AntdUI.Input();
            this.joinroom = new AntdUI.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.divider2 = new AntdUI.Divider();
            this.dhcpjoin = new AntdUI.Input();
            this.button1 = new AntdUI.Button();
            this.createroom = new AntdUI.Button();
            this.button2 = new AntdUI.Button();
            this.button3 = new AntdUI.Button();
            this.divider1 = new AntdUI.Divider();
            this.SuspendLayout();
            // 
            // create
            // 
            this.create.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(188)))), ((int)(((byte)(188)))));
            this.create.BadgeAlign = AntdUI.TAlignFrom.Bottom;
            this.create.Cursor = System.Windows.Forms.Cursors.Hand;
            this.create.Font = new System.Drawing.Font("微软雅黑", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.create.IconSvg = "LinkOutlined";
            this.create.Location = new System.Drawing.Point(562, 174);
            this.create.Name = "create";
            this.create.Size = new System.Drawing.Size(192, 62);
            this.create.TabIndex = 18;
            this.create.Text = "创建网络";
            this.create.Click += new System.EventHandler(this.create_Click);
            // 
            // networkname
            // 
            this.networkname.Depth = 0;
            this.networkname.Hint = "";
            this.networkname.Location = new System.Drawing.Point(562, 60);
            this.networkname.MouseState = MaterialSkin.MouseState.HOVER;
            this.networkname.Name = "networkname";
            this.networkname.PasswordChar = '\0';
            this.networkname.SelectedText = "";
            this.networkname.SelectionLength = 0;
            this.networkname.SelectionStart = 0;
            this.networkname.Size = new System.Drawing.Size(148, 23);
            this.networkname.TabIndex = 19;
            this.networkname.Text = "NetworkName";
            this.networkname.UseSystemPasswordChar = false;
            // 
            // pass
            // 
            this.pass.Depth = 0;
            this.pass.Hint = "";
            this.pass.Location = new System.Drawing.Point(562, 99);
            this.pass.MouseState = MaterialSkin.MouseState.HOVER;
            this.pass.Name = "pass";
            this.pass.PasswordChar = '\0';
            this.pass.SelectedText = "";
            this.pass.SelectionLength = 0;
            this.pass.SelectionStart = 0;
            this.pass.Size = new System.Drawing.Size(148, 23);
            this.pass.TabIndex = 20;
            this.pass.Text = "Password";
            this.pass.UseSystemPasswordChar = false;
            // 
            // dhcp
            // 
            this.dhcp.Depth = 0;
            this.dhcp.Hint = "";
            this.dhcp.Location = new System.Drawing.Point(562, 145);
            this.dhcp.MouseState = MaterialSkin.MouseState.HOVER;
            this.dhcp.Name = "dhcp";
            this.dhcp.PasswordChar = '\0';
            this.dhcp.SelectedText = "";
            this.dhcp.SelectionLength = 0;
            this.dhcp.SelectionStart = 0;
            this.dhcp.Size = new System.Drawing.Size(148, 23);
            this.dhcp.TabIndex = 21;
            this.dhcp.Text = "DHCP\\Address";
            this.dhcp.UseSystemPasswordChar = false;
            // 
            // roomname
            // 
            this.roomname.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.roomname.Location = new System.Drawing.Point(3, 89);
            this.roomname.Name = "roomname";
            this.roomname.Size = new System.Drawing.Size(192, 47);
            this.roomname.TabIndex = 24;
            this.roomname.Text = "房间名称";
            // 
            // roompwd
            // 
            this.roompwd.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.roompwd.Location = new System.Drawing.Point(3, 149);
            this.roompwd.Name = "roompwd";
            this.roompwd.Size = new System.Drawing.Size(192, 47);
            this.roompwd.TabIndex = 25;
            this.roompwd.Text = "房间密码(可选)";
            // 
            // TopText
            // 
            this.TopText.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.TopText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TopText.Cursor = System.Windows.Forms.Cursors.AppStarting;
            this.TopText.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TopText.Location = new System.Drawing.Point(132, 15);
            this.TopText.Multiline = false;
            this.TopText.Name = "TopText";
            this.TopText.ReadOnly = true;
            this.TopText.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.TopText.Size = new System.Drawing.Size(622, 32);
            this.TopText.TabIndex = 26;
            this.TopText.Text = "该未成形联机方式将在未来版本更新为其他联机方式,现不推荐使用";
            // 
            // progress1
            // 
            this.progress1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.progress1.Location = new System.Drawing.Point(222, 0);
            this.progress1.Name = "progress1";
            this.progress1.Size = new System.Drawing.Size(378, 17);
            this.progress1.TabIndex = 46;
            this.progress1.Text = "progress1";
            this.progress1.Visible = false;
            // 
            // roompwdjoin
            // 
            this.roompwdjoin.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.roompwdjoin.Location = new System.Drawing.Point(288, 149);
            this.roompwdjoin.Name = "roompwdjoin";
            this.roompwdjoin.Size = new System.Drawing.Size(192, 47);
            this.roompwdjoin.TabIndex = 49;
            this.roompwdjoin.Text = "房间密码(可选)";
            // 
            // roomnamejoin
            // 
            this.roomnamejoin.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.roomnamejoin.Location = new System.Drawing.Point(288, 89);
            this.roomnamejoin.Name = "roomnamejoin";
            this.roomnamejoin.Size = new System.Drawing.Size(192, 47);
            this.roomnamejoin.TabIndex = 48;
            this.roomnamejoin.Text = "房间名称";
            // 
            // joinroom
            // 
            this.joinroom.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(188)))), ((int)(((byte)(188)))));
            this.joinroom.BadgeAlign = AntdUI.TAlignFrom.Bottom;
            this.joinroom.Cursor = System.Windows.Forms.Cursors.Hand;
            this.joinroom.Font = new System.Drawing.Font("微软雅黑", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.joinroom.IconSvg = "LinkOutlined";
            this.joinroom.Location = new System.Drawing.Point(288, 211);
            this.joinroom.Name = "joinroom";
            this.joinroom.Size = new System.Drawing.Size(192, 62);
            this.joinroom.TabIndex = 47;
            this.joinroom.Text = "加入房间";
            this.joinroom.Click += new System.EventHandler(this.joinroom_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.richTextBox1.Location = new System.Drawing.Point(0, 279);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(762, 176);
            this.richTextBox1.TabIndex = 50;
            this.richTextBox1.Text = "";
            // 
            // divider2
            // 
            this.divider2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.divider2.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.divider2.Location = new System.Drawing.Point(3, 60);
            this.divider2.Name = "divider2";
            this.divider2.Size = new System.Drawing.Size(218, 23);
            this.divider2.TabIndex = 30;
            this.divider2.Text = "创建并开启联机房间";
            // 
            // dhcpjoin
            // 
            this.dhcpjoin.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dhcpjoin.Location = new System.Drawing.Point(288, 42);
            this.dhcpjoin.Name = "dhcpjoin";
            this.dhcpjoin.Size = new System.Drawing.Size(192, 47);
            this.dhcpjoin.TabIndex = 51;
            this.dhcpjoin.Text = "10.0.0.2/24";
            // 
            // button1
            // 
            this.button1.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(188)))), ((int)(((byte)(188)))));
            this.button1.BadgeAlign = AntdUI.TAlignFrom.Bottom;
            this.button1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button1.Font = new System.Drawing.Font("微软雅黑", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button1.IconSvg = "DisconnectOutlined";
            this.button1.Location = new System.Drawing.Point(488, 211);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(68, 62);
            this.button1.TabIndex = 52;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // createroom
            // 
            this.createroom.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(188)))), ((int)(((byte)(188)))));
            this.createroom.BadgeAlign = AntdUI.TAlignFrom.Bottom;
            this.createroom.Cursor = System.Windows.Forms.Cursors.Hand;
            this.createroom.Font = new System.Drawing.Font("微软雅黑", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.createroom.IconSvg = "LinkOutlined";
            this.createroom.Location = new System.Drawing.Point(3, 211);
            this.createroom.Name = "createroom";
            this.createroom.Size = new System.Drawing.Size(192, 62);
            this.createroom.TabIndex = 22;
            this.createroom.Text = "创建房间";
            this.createroom.Click += new System.EventHandler(this.createroom_Click);
            // 
            // button2
            // 
            this.button2.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(188)))), ((int)(((byte)(188)))));
            this.button2.BadgeAlign = AntdUI.TAlignFrom.Bottom;
            this.button2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button2.Font = new System.Drawing.Font("微软雅黑", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button2.IconSvg = "CloudDownloadOutlined";
            this.button2.Location = new System.Drawing.Point(0, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(53, 44);
            this.button2.TabIndex = 53;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(188)))), ((int)(((byte)(188)))));
            this.button3.BadgeAlign = AntdUI.TAlignFrom.Bottom;
            this.button3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button3.Font = new System.Drawing.Font("微软雅黑", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button3.IconSvg = "QuestionCircleOutlined";
            this.button3.Location = new System.Drawing.Point(56, 3);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(53, 44);
            this.button3.TabIndex = 54;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // divider1
            // 
            this.divider1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.divider1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.divider1.Location = new System.Drawing.Point(574, 242);
            this.divider1.Name = "divider1";
            this.divider1.Size = new System.Drawing.Size(188, 31);
            this.divider1.TabIndex = 31;
            this.divider1.Text = "大概率废了,Candy效果不太行";
            // 
            // ZTLobby
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.divider1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dhcpjoin);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.roompwdjoin);
            this.Controls.Add(this.roomnamejoin);
            this.Controls.Add(this.joinroom);
            this.Controls.Add(this.progress1);
            this.Controls.Add(this.divider2);
            this.Controls.Add(this.TopText);
            this.Controls.Add(this.roompwd);
            this.Controls.Add(this.roomname);
            this.Controls.Add(this.createroom);
            this.Controls.Add(this.dhcp);
            this.Controls.Add(this.pass);
            this.Controls.Add(this.networkname);
            this.Controls.Add(this.create);
            this.Name = "ZTLobby";
            this.Size = new System.Drawing.Size(762, 455);
            this.Load += new System.EventHandler(this.ZTLobby_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private AntdUI.Button create;
        private MaterialSkin.Controls.MaterialSingleLineTextField networkname;
        private MaterialSkin.Controls.MaterialSingleLineTextField pass;
        private MaterialSkin.Controls.MaterialSingleLineTextField dhcp;
        private AntdUI.Input roomname;
        private AntdUI.Input roompwd;
        private System.Windows.Forms.RichTextBox TopText;
        private AntdUI.Progress progress1;
        private AntdUI.Input roompwdjoin;
        private AntdUI.Input roomnamejoin;
        private AntdUI.Button joinroom;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private AntdUI.Divider divider2;
        private AntdUI.Input dhcpjoin;
        private AntdUI.Button button1;
        private AntdUI.Button createroom;
        private AntdUI.Button button2;
        private AntdUI.Button button3;
        private AntdUI.Divider divider1;
    }
}
