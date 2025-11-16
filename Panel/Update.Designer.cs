namespace MinecraftConnectTool
{
    partial class Update
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
            this.button2 = new AntdUI.Button();
            this.button1 = new AntdUI.Button();
            this.alert1 = new AntdUI.Alert();
            this.label1 = new AntdUI.Label();
            this.alert2 = new AntdUI.Alert();
            this.label2 = new AntdUI.Label();
            this.label3 = new AntdUI.Label();
            this.label4 = new AntdUI.Label();
            this.button3 = new AntdUI.Button();
            this.progress1 = new AntdUI.Progress();
            this.button4 = new AntdUI.Button();
            this.button5 = new AntdUI.Button();
            this.alert2.SuspendLayout();
            this.SuspendLayout();
            // 
            // button2
            // 
            this.button2.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(174)))), ((int)(((byte)(213)))), ((int)(((byte)(229)))));
            this.button2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button2.IconSvg = "BlockOutlined";
            this.button2.Location = new System.Drawing.Point(510, 395);
            this.button2.Name = "button2";
            this.button2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button2.Size = new System.Drawing.Size(110, 52);
            this.button2.TabIndex = 36;
            this.button2.Text = "调试按钮";
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.BackActive = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(184)))), ((int)(((byte)(245)))));
            this.button1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button1.IconSvg = "ReloadOutlined";
            this.button1.Location = new System.Drawing.Point(639, 395);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(110, 52);
            this.button1.TabIndex = 0;
            this.button1.Text = "检查更新";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // alert1
            // 
            this.alert1.BorderWidth = 1F;
            this.alert1.Cursor = System.Windows.Forms.Cursors.No;
            this.alert1.Font = new System.Drawing.Font("微软雅黑", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.alert1.Icon = AntdUI.TType.Info;
            this.alert1.Location = new System.Drawing.Point(0, 395);
            this.alert1.Name = "alert1";
            this.alert1.Size = new System.Drawing.Size(490, 60);
            this.alert1.TabIndex = 38;
            this.alert1.Text = "测试用户如需降级回正式版，直接在设置中关闭接收测试更新并重新检查更新即可";
            this.alert1.TextTitle = "如需使用测试通道，请在设置中开启接收测试更新";
            this.alert1.Click += new System.EventHandler(this.alert1_Click);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(116)))), ((int)(((byte)(116)))), ((int)(((byte)(186)))));
            this.label1.IconRatio = 0.734F;
            this.label1.Location = new System.Drawing.Point(352, 18);
            this.label1.Name = "label1";
            this.label1.PrefixSvg = "ProductFilled";
            this.label1.Size = new System.Drawing.Size(401, 36);
            this.label1.SuffixSvg = "";
            this.label1.TabIndex = 39;
            this.label1.Text = "更新日志";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // alert2
            // 
            this.alert2.BadgeOffsetX = 3;
            this.alert2.BadgeSize = 0.7F;
            this.alert2.BorderWidth = 1F;
            this.alert2.Controls.Add(this.button5);
            this.alert2.Controls.Add(this.button4);
            this.alert2.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.alert2.Icon = AntdUI.TType.Info;
            this.alert2.Location = new System.Drawing.Point(352, 47);
            this.alert2.Name = "alert2";
            this.alert2.Radius = 15;
            this.alert2.Size = new System.Drawing.Size(401, 336);
            this.alert2.TabIndex = 40;
            this.alert2.Text = "喵喵喵？检查一下更新？";
            this.alert2.TextTitle = "";
            this.alert2.Click += new System.EventHandler(this.alert2_Click);
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.IconRatio = 1.5F;
            this.label2.Location = new System.Drawing.Point(12, 47);
            this.label2.Name = "label2";
            this.label2.PrefixSvg = "ProductFilled";
            this.label2.Size = new System.Drawing.Size(334, 70);
            this.label2.TabIndex = 41;
            this.label2.Text = "当前版本:获取中";
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.IconRatio = 1.5F;
            this.label3.Location = new System.Drawing.Point(12, 124);
            this.label3.Name = "label3";
            this.label3.PrefixSvg = "AppstoreFilled";
            this.label3.Size = new System.Drawing.Size(334, 70);
            this.label3.TabIndex = 42;
            this.label3.Text = "云版本号:获取中";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(167)))), ((int)(((byte)(255)))));
            this.label4.IconRatio = 1.5F;
            this.label4.Location = new System.Drawing.Point(0, 363);
            this.label4.Name = "label4";
            this.label4.PrefixColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(167)))), ((int)(((byte)(255)))));
            this.label4.PrefixSvg = "ApiFilled";
            this.label4.Size = new System.Drawing.Size(363, 26);
            this.label4.TabIndex = 43;
            this.label4.Text = "当前更新通道：默认";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // button3
            // 
            this.button3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button3.IconSvg = "ArrowUpOutlined";
            this.button3.Location = new System.Drawing.Point(78, 299);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(182, 52);
            this.button3.TabIndex = 44;
            this.button3.Text = "立即更新";
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // progress1
            // 
            this.progress1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.progress1.Location = new System.Drawing.Point(0, 0);
            this.progress1.Name = "progress1";
            this.progress1.Size = new System.Drawing.Size(762, 11);
            this.progress1.TabIndex = 45;
            this.progress1.Text = "progress1";
            this.progress1.Visible = false;
            // 
            // button4
            // 
            this.button4.Ghost = true;
            this.button4.IconRatio = 0.8F;
            this.button4.IconSvg = "UpCircleOutlined";
            this.button4.Location = new System.Drawing.Point(366, 271);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(32, 32);
            this.button4.TabIndex = 0;
            this.button4.Visible = false;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button5
            // 
            this.button5.Ghost = true;
            this.button5.IconRatio = 0.8F;
            this.button5.IconSvg = "DownCircleOutlined";
            this.button5.Location = new System.Drawing.Point(366, 302);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(32, 32);
            this.button5.TabIndex = 46;
            this.button5.Visible = false;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // Update
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.progress1);
            this.Controls.Add(this.alert2);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.alert1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Update";
            this.Size = new System.Drawing.Size(762, 455);
            this.Load += new System.EventHandler(this.Update_Load);
            this.alert2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Button button2;
        private AntdUI.Button button1;
        private AntdUI.Alert alert1;
        private AntdUI.Label label1;
        private AntdUI.Alert alert2;
        private AntdUI.Label label2;
        private AntdUI.Label label3;
        private AntdUI.Label label4;
        private AntdUI.Button button3;
        private AntdUI.Progress progress1;
        private AntdUI.Button button4;
        private AntdUI.Button button5;
    }
}
