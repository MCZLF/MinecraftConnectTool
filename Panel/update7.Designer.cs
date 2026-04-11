namespace MinecraftConnectTool
{
    partial class update7
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
            AntdUI.StepsItem stepsItem1 = new AntdUI.StepsItem();
            AntdUI.StepsItem stepsItem2 = new AntdUI.StepsItem();
            AntdUI.StepsItem stepsItem3 = new AntdUI.StepsItem();
            AntdUI.StepsItem stepsItem4 = new AntdUI.StepsItem();
            AntdUI.StepsItem stepsItem5 = new AntdUI.StepsItem();
            this.alert1 = new AntdUI.Alert();
            this.TopText = new System.Windows.Forms.RichTextBox();
            this.steps1 = new AntdUI.Steps();
            this.button4 = new AntdUI.Button();
            this.detailText = new AntdUI.Label();
            this.progressBar = new AntdUI.Progress();
            this.progressLabel = new AntdUI.Label();
            this.button1 = new AntdUI.Button();
            this.SuspendLayout();
            // 
            // alert1
            // 
            this.alert1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.alert1.Icon = AntdUI.TType.Warn;
            this.alert1.Location = new System.Drawing.Point(0, 414);
            this.alert1.Name = "alert1";
            this.alert1.Size = new System.Drawing.Size(762, 41);
            this.alert1.TabIndex = 0;
            this.alert1.Text = "MinecraftConnectTool 0.0.7已完全不支持Windows7,如果您是Win7用户请勿升级";
            // 
            // TopText
            // 
            this.TopText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TopText.Cursor = System.Windows.Forms.Cursors.AppStarting;
            this.TopText.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TopText.ForeColor = System.Drawing.SystemColors.InfoText;
            this.TopText.Location = new System.Drawing.Point(17, 3);
            this.TopText.Multiline = false;
            this.TopText.Name = "TopText";
            this.TopText.ReadOnly = true;
            this.TopText.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.TopText.Size = new System.Drawing.Size(529, 32);
            this.TopText.TabIndex = 24;
            this.TopText.Text = "0.0.7升级向导";
            // 
            // steps1
            // 
            this.steps1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.steps1.Gap = 6;
            stepsItem1.IconSvg = "";
            stepsItem1.SubTitle = "阅读并确认升级信息";
            stepsItem1.Title = "阅读升级须知";
            stepsItem2.SubTitle = "验证系统版本是否支持";
            stepsItem2.Title = "验证操作系统版本";
            stepsItem3.SubTitle = "0.0.7使用.NET8从底层重写";
            stepsItem3.Title = "验证运行环境";
            stepsItem4.SubTitle = "根据系统自动选择32/64位";
            stepsItem4.Title = "下载新版本";
            stepsItem5.SubTitle = "自动重启到新版本";
            stepsItem5.Title = "重启到新版本";
            this.steps1.Items.Add(stepsItem1);
            this.steps1.Items.Add(stepsItem2);
            this.steps1.Items.Add(stepsItem3);
            this.steps1.Items.Add(stepsItem4);
            this.steps1.Items.Add(stepsItem5);
            this.steps1.Location = new System.Drawing.Point(17, 41);
            this.steps1.Name = "steps1";
            this.steps1.Size = new System.Drawing.Size(379, 357);
            this.steps1.Status = AntdUI.TStepState.Wait;
            this.steps1.TabIndex = 25;
            this.steps1.Text = "steps1";
            this.steps1.Vertical = true;
            this.steps1.ItemClick += new AntdUI.StepsItemEventHandler(this.steps1_ItemClick);
            // 
            // button4
            // 
            this.button4.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button4.IconSvg = "SendOutlined";
            this.button4.Location = new System.Drawing.Point(616, 333);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(124, 46);
            this.button4.TabIndex = 31;
            this.button4.Text = "开始升级";
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // detailText
            // 
            this.detailText.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this.detailText.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.detailText.Location = new System.Drawing.Point(402, 41);
            this.detailText.Name = "detailText";
            this.detailText.Size = new System.Drawing.Size(0, 0);
            this.detailText.TabIndex = 32;
            this.detailText.Text = "";
            // 
            // progressBar
            // 
            this.progressBar.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.progressBar.Location = new System.Drawing.Point(402, 270);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(338, 20);
            this.progressBar.TabIndex = 33;
            this.progressBar.Visible = false;
            // 
            // progressLabel
            // 
            this.progressLabel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.progressLabel.Location = new System.Drawing.Point(402, 293);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new System.Drawing.Size(338, 23);
            this.progressLabel.TabIndex = 34;
            this.progressLabel.Text = "";
            this.progressLabel.Visible = false;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button1.IconSvg = "DownloadOutlined";
            this.button1.Location = new System.Drawing.Point(486, 333);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(124, 46);
            this.button1.TabIndex = 35;
            this.button1.Text = "手动升级";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // update7
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button1);
            this.Controls.Add(this.progressLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.detailText);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.steps1);
            this.Controls.Add(this.TopText);
            this.Controls.Add(this.alert1);
            this.Name = "update7";
            this.Size = new System.Drawing.Size(762, 455);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AntdUI.Alert alert1;
        private System.Windows.Forms.RichTextBox TopText;
        private AntdUI.Steps steps1;
        private AntdUI.Button button4;
        private AntdUI.Label detailText;
        private AntdUI.Progress progressBar;
        private AntdUI.Label progressLabel;
        private AntdUI.Button button1;
    }
}
