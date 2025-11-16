namespace MinecraftConnectTool
{
    partial class P2PMode
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
            this.divider2 = new AntdUI.Divider();
            this.Divider1 = new AntdUI.Divider();
            this.materialSingleLineTextField3 = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.TopText = new System.Windows.Forms.RichTextBox();
            this.Joiner = new AntdUI.Button();
            this.select = new AntdUI.Button();
            this.materialSingleLineTextField2 = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.materialSingleLineTextField1 = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.Opener = new AntdUI.Button();
            this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
            this.button1 = new AntdUI.Button();
            this.materialSingleLineTextField4 = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.badge1 = new AntdUI.Badge();
            this.button4 = new AntdUI.Button();
            this.badge2 = new AntdUI.Badge();
            this.infobutton = new MaterialSkin.Controls.MaterialFlatButton();
            this.alert1 = new AntdUI.Alert();
            this.badge3 = new AntdUI.Badge();
            this.progress1 = new AntdUI.Progress();
            this.badge4 = new AntdUI.Badge();
            this.badge5 = new AntdUI.Badge();
            this.materialLabel1 = new MaterialSkin.Controls.MaterialLabel();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.alert1.SuspendLayout();
            this.SuspendLayout();
            // 
            // divider2
            // 
            this.divider2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.divider2.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.divider2.Location = new System.Drawing.Point(10, 91);
            this.divider2.Name = "divider2";
            this.divider2.Size = new System.Drawing.Size(218, 23);
            this.divider2.TabIndex = 29;
            this.divider2.Text = "开启联机房间";
            this.divider2.Click += new System.EventHandler(this.divider2_Click);
            // 
            // Divider1
            // 
            this.Divider1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Divider1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.Divider1.Location = new System.Drawing.Point(10, 215);
            this.Divider1.Name = "Divider1";
            this.Divider1.Size = new System.Drawing.Size(218, 23);
            this.Divider1.TabIndex = 28;
            this.Divider1.Text = "加入联机房间";
            // 
            // materialSingleLineTextField3
            // 
            this.materialSingleLineTextField3.Cursor = System.Windows.Forms.Cursors.UpArrow;
            this.materialSingleLineTextField3.Depth = 0;
            this.materialSingleLineTextField3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialSingleLineTextField3.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.materialSingleLineTextField3.Hint = "";
            this.materialSingleLineTextField3.Location = new System.Drawing.Point(34, 308);
            this.materialSingleLineTextField3.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialSingleLineTextField3.Name = "materialSingleLineTextField3";
            this.materialSingleLineTextField3.PasswordChar = '\0';
            this.materialSingleLineTextField3.SelectedText = "";
            this.materialSingleLineTextField3.SelectionLength = 0;
            this.materialSingleLineTextField3.SelectionStart = 0;
            this.materialSingleLineTextField3.Size = new System.Drawing.Size(172, 23);
            this.materialSingleLineTextField3.TabIndex = 24;
            this.materialSingleLineTextField3.Text = "输入目标端口";
            this.materialSingleLineTextField3.UseSystemPasswordChar = false;
            this.materialSingleLineTextField3.Click += new System.EventHandler(this.materialSingleLineTextField3_Click);
            // 
            // TopText
            // 
            this.TopText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TopText.Cursor = System.Windows.Forms.Cursors.AppStarting;
            this.TopText.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TopText.ForeColor = System.Drawing.SystemColors.InfoText;
            this.TopText.Location = new System.Drawing.Point(222, 15);
            this.TopText.Multiline = false;
            this.TopText.Name = "TopText";
            this.TopText.ReadOnly = true;
            this.TopText.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.TopText.Size = new System.Drawing.Size(529, 32);
            this.TopText.TabIndex = 23;
            this.TopText.Text = "您正在使用P2P模式进行联机ヾ(≧▽≦*)o";
            this.TopText.TextChanged += new System.EventHandler(this.TopText_TextChanged);
            // 
            // Joiner
            // 
            this.Joiner.Font = new System.Drawing.Font("微软雅黑", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Joiner.Location = new System.Drawing.Point(34, 340);
            this.Joiner.Name = "Joiner";
            this.Joiner.Radius = 5;
            this.Joiner.Shape = AntdUI.TShape.Round;
            this.Joiner.Size = new System.Drawing.Size(172, 77);
            this.Joiner.TabIndex = 22;
            this.Joiner.Text = "加入联机房间";
            this.Joiner.Click += new System.EventHandler(this.Joiner_Click);
            // 
            // select
            // 
            this.select.Location = new System.Drawing.Point(726, 458);
            this.select.Name = "select";
            this.select.Size = new System.Drawing.Size(56, 23);
            this.select.TabIndex = 20;
            this.select.Text = "Test";
            // 
            // materialSingleLineTextField2
            // 
            this.materialSingleLineTextField2.Cursor = System.Windows.Forms.Cursors.UpArrow;
            this.materialSingleLineTextField2.Depth = 0;
            this.materialSingleLineTextField2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialSingleLineTextField2.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.materialSingleLineTextField2.Hint = "";
            this.materialSingleLineTextField2.Location = new System.Drawing.Point(34, 256);
            this.materialSingleLineTextField2.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialSingleLineTextField2.Name = "materialSingleLineTextField2";
            this.materialSingleLineTextField2.PasswordChar = '\0';
            this.materialSingleLineTextField2.SelectedText = "";
            this.materialSingleLineTextField2.SelectionLength = 0;
            this.materialSingleLineTextField2.SelectionStart = 0;
            this.materialSingleLineTextField2.Size = new System.Drawing.Size(172, 23);
            this.materialSingleLineTextField2.TabIndex = 18;
            this.materialSingleLineTextField2.Text = "输入提示码";
            this.materialSingleLineTextField2.UseSystemPasswordChar = false;
            this.materialSingleLineTextField2.Click += new System.EventHandler(this.materialSingleLineTextField2_Click);
            // 
            // materialSingleLineTextField1
            // 
            this.materialSingleLineTextField1.Depth = 0;
            this.materialSingleLineTextField1.Hint = "";
            this.materialSingleLineTextField1.Location = new System.Drawing.Point(0, 458);
            this.materialSingleLineTextField1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialSingleLineTextField1.Name = "materialSingleLineTextField1";
            this.materialSingleLineTextField1.PasswordChar = '\0';
            this.materialSingleLineTextField1.SelectedText = "";
            this.materialSingleLineTextField1.SelectionLength = 0;
            this.materialSingleLineTextField1.SelectionStart = 0;
            this.materialSingleLineTextField1.Size = new System.Drawing.Size(621, 23);
            this.materialSingleLineTextField1.TabIndex = 17;
            this.materialSingleLineTextField1.Text = "不太清楚为什么你能够看到这行字，但是应该就是分辨率和缩放的问题了";
            this.materialSingleLineTextField1.UseSystemPasswordChar = false;
            // 
            // Opener
            // 
            this.Opener.Font = new System.Drawing.Font("微软雅黑", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Opener.Location = new System.Drawing.Point(34, 120);
            this.Opener.Name = "Opener";
            this.Opener.Radius = 5;
            this.Opener.Shape = AntdUI.TShape.Round;
            this.Opener.Size = new System.Drawing.Size(172, 78);
            this.Opener.TabIndex = 33;
            this.Opener.Text = "开启联机房间";
            this.Opener.Click += new System.EventHandler(this.Opener_Click);
            // 
            // richTextBoxLog
            // 
            this.richTextBoxLog.BackColor = System.Drawing.SystemColors.Info;
            this.richTextBoxLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxLog.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.richTextBoxLog.Location = new System.Drawing.Point(224, 114);
            this.richTextBoxLog.Name = "richTextBoxLog";
            this.richTextBoxLog.ReadOnly = true;
            this.richTextBoxLog.Size = new System.Drawing.Size(533, 306);
            this.richTextBoxLog.TabIndex = 34;
            this.richTextBoxLog.Text = "";
            this.richTextBoxLog.TextChanged += new System.EventHandler(this.richTextBoxLog_TextChanged_1);
            // 
            // button1
            // 
            this.button1.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(174)))), ((int)(((byte)(213)))), ((int)(((byte)(229)))));
            this.button1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button1.IconSvg = "BlockOutlined";
            this.button1.Location = new System.Drawing.Point(3, 3);
            this.button1.Name = "button1";
            this.button1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button1.Size = new System.Drawing.Size(106, 44);
            this.button1.TabIndex = 35;
            this.button1.Text = "协助调试";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // materialSingleLineTextField4
            // 
            this.materialSingleLineTextField4.Depth = 0;
            this.materialSingleLineTextField4.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialSingleLineTextField4.Hint = "";
            this.materialSingleLineTextField4.Location = new System.Drawing.Point(10, 429);
            this.materialSingleLineTextField4.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialSingleLineTextField4.Name = "materialSingleLineTextField4";
            this.materialSingleLineTextField4.PasswordChar = '\0';
            this.materialSingleLineTextField4.SelectedText = "";
            this.materialSingleLineTextField4.SelectionLength = 0;
            this.materialSingleLineTextField4.SelectionStart = 0;
            this.materialSingleLineTextField4.Size = new System.Drawing.Size(611, 23);
            this.materialSingleLineTextField4.TabIndex = 36;
            this.materialSingleLineTextField4.Text = "请勿多开核心,避免出现意料之外的错误！启动后点击上方提示按钮,即可复制相关信息";
            this.materialSingleLineTextField4.UseSystemPasswordChar = false;
            this.materialSingleLineTextField4.Click += new System.EventHandler(this.materialSingleLineTextField4_Click);
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Enabled = false;
            this.checkBox2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.checkBox2.Location = new System.Drawing.Point(658, 431);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(99, 21);
            this.checkBox2.TabIndex = 37;
            this.checkBox2.Text = "使用旧版方式";
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // badge1
            // 
            this.badge1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.badge1.Location = new System.Drawing.Point(330, 94);
            this.badge1.Name = "badge1";
            this.badge1.Size = new System.Drawing.Size(121, 20);
            this.badge1.State = AntdUI.TState.Success;
            this.badge1.TabIndex = 38;
            this.badge1.Text = "已启用自定义端口";
            this.badge1.Visible = false;
            // 
            // button4
            // 
            this.button4.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button4.Location = new System.Drawing.Point(663, 49);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(94, 44);
            this.button4.TabIndex = 30;
            this.button4.Text = "下载核心";
            this.button4.Visible = false;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // badge2
            // 
            this.badge2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.badge2.Location = new System.Drawing.Point(219, 94);
            this.badge2.Name = "badge2";
            this.badge2.Size = new System.Drawing.Size(139, 20);
            this.badge2.State = AntdUI.TState.Success;
            this.badge2.TabIndex = 39;
            this.badge2.Text = "已启用房间多播";
            this.badge2.Visible = false;
            // 
            // infobutton
            // 
            this.infobutton.AutoSize = true;
            this.infobutton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.infobutton.BackColor = System.Drawing.Color.Cyan;
            this.infobutton.Depth = 0;
            this.infobutton.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.infobutton.Location = new System.Drawing.Point(117, 6);
            this.infobutton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.infobutton.MouseState = MaterialSkin.MouseState.HOVER;
            this.infobutton.Name = "infobutton";
            this.infobutton.Primary = false;
            this.infobutton.Size = new System.Drawing.Size(172, 36);
            this.infobutton.TabIndex = 40;
            this.infobutton.Text = "materialFlatButton1";
            this.infobutton.UseVisualStyleBackColor = false;
            this.infobutton.Visible = false;
            this.infobutton.Click += new System.EventHandler(this.infobutton_Click);
            // 
            // alert1
            // 
            this.alert1.BorderWidth = 0.5F;
            this.alert1.Controls.Add(this.infobutton);
            this.alert1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.alert1.Icon = AntdUI.TType.Info;
            this.alert1.Location = new System.Drawing.Point(224, 43);
            this.alert1.Name = "alert1";
            this.alert1.Size = new System.Drawing.Size(365, 45);
            this.alert1.TabIndex = 41;
            this.alert1.Text = "提示码";
            this.alert1.Visible = false;
            this.alert1.Click += new System.EventHandler(this.alert1_Click);
            // 
            // badge3
            // 
            this.badge3.DotRatio = 0.65F;
            this.badge3.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.badge3.Location = new System.Drawing.Point(4, 43);
            this.badge3.Name = "badge3";
            this.badge3.Size = new System.Drawing.Size(214, 23);
            this.badge3.TabIndex = 42;
            this.badge3.Text = "正常状态下你能看到这行字是bug";
            this.badge3.Visible = false;
            // 
            // progress1
            // 
            this.progress1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.progress1.Location = new System.Drawing.Point(222, 0);
            this.progress1.Name = "progress1";
            this.progress1.Size = new System.Drawing.Size(378, 17);
            this.progress1.TabIndex = 45;
            this.progress1.Text = "progress1";
            this.progress1.Visible = false;
            // 
            // badge4
            // 
            this.badge4.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.badge4.Location = new System.Drawing.Point(450, 94);
            this.badge4.Name = "badge4";
            this.badge4.Size = new System.Drawing.Size(139, 20);
            this.badge4.State = AntdUI.TState.Success;
            this.badge4.TabIndex = 46;
            this.badge4.Text = "已启用兼容模式Olan";
            this.badge4.Visible = false;
            // 
            // badge5
            // 
            this.badge5.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.badge5.Location = new System.Drawing.Point(587, 94);
            this.badge5.Name = "badge5";
            this.badge5.Size = new System.Drawing.Size(164, 20);
            this.badge5.State = AntdUI.TState.Success;
            this.badge5.TabIndex = 47;
            this.badge5.Text = "已启用P2P传输优化(TEST)";
            this.badge5.Visible = false;
            // 
            // materialLabel1
            // 
            this.materialLabel1.AutoSize = true;
            this.materialLabel1.Depth = 0;
            this.materialLabel1.Font = new System.Drawing.Font("Roboto", 11F);
            this.materialLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialLabel1.Location = new System.Drawing.Point(4, 66);
            this.materialLabel1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabel1.Name = "materialLabel1";
            this.materialLabel1.Size = new System.Drawing.Size(75, 19);
            this.materialLabel1.TabIndex = 48;
            this.materialLabel1.Text = "Ping:-1ms";
            this.materialLabel1.Visible = false;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(622, 462);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(96, 16);
            this.checkBox1.TabIndex = 25;
            this.checkBox1.Text = "使用旧版方式";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // P2PMode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.badge1);
            this.Controls.Add(this.badge5);
            this.Controls.Add(this.materialLabel1);
            this.Controls.Add(this.badge3);
            this.Controls.Add(this.badge4);
            this.Controls.Add(this.progress1);
            this.Controls.Add(this.alert1);
            this.Controls.Add(this.TopText);
            this.Controls.Add(this.badge2);
            this.Controls.Add(this.richTextBoxLog);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.materialSingleLineTextField4);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Opener);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.divider2);
            this.Controls.Add(this.Divider1);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.materialSingleLineTextField3);
            this.Controls.Add(this.Joiner);
            this.Controls.Add(this.select);
            this.Controls.Add(this.materialSingleLineTextField2);
            this.Controls.Add(this.materialSingleLineTextField1);
            this.Name = "P2PMode";
            this.Size = new System.Drawing.Size(762, 455);
            this.Load += new System.EventHandler(this.P2PMode_Load);
            this.alert1.ResumeLayout(false);
            this.alert1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private AntdUI.Divider divider2;
        private AntdUI.Divider Divider1;
        private MaterialSkin.Controls.MaterialSingleLineTextField materialSingleLineTextField3;
        private System.Windows.Forms.RichTextBox TopText;
        private AntdUI.Button Joiner;
        private AntdUI.Button select;
        private MaterialSkin.Controls.MaterialSingleLineTextField materialSingleLineTextField2;
        private MaterialSkin.Controls.MaterialSingleLineTextField materialSingleLineTextField1;
        private AntdUI.Button Opener;
        private System.Windows.Forms.RichTextBox richTextBoxLog;
        private AntdUI.Button button1;
        private MaterialSkin.Controls.MaterialSingleLineTextField materialSingleLineTextField4;
        private System.Windows.Forms.CheckBox checkBox2;
        private AntdUI.Badge badge1;
        private AntdUI.Button button4;
        private AntdUI.Badge badge2;
        private MaterialSkin.Controls.MaterialFlatButton infobutton;
        private AntdUI.Alert alert1;
        private AntdUI.Badge badge3;
        private AntdUI.Progress progress1;
        private AntdUI.Badge badge4;
        private AntdUI.Badge badge5;
        private MaterialSkin.Controls.MaterialLabel materialLabel1;
        private System.Windows.Forms.CheckBox checkBox1;
    }
}
