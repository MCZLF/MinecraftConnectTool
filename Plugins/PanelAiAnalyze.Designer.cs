namespace MinecraftConnectTool
{
    partial class PanelAiAnalyze
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelTop = new System.Windows.Forms.Panel();
            this.buttonBack = new AntdUI.Button();
            this.labelTitle = new AntdUI.Label();
            this.buttonCopy = new AntdUI.Button();
            this.buttonRetry = new AntdUI.Button();
            this.panelContent = new System.Windows.Forms.Panel();
            this.panelLoading = new System.Windows.Forms.Panel();
            this.panelSpin = new System.Windows.Forms.Panel();
            this.labelLoading = new AntdUI.Label();
            this.labelSource = new AntdUI.Label();
            this.spinTimer = new System.Windows.Forms.Timer(this.components);
            this.panelError = new System.Windows.Forms.Panel();
            this.labelError = new AntdUI.Label();
            this.buttonErrorRetry = new AntdUI.Button();
            this.panelResult = new System.Windows.Forms.Panel();
            this.panelInfoTags = new System.Windows.Forms.FlowLayoutPanel();
            this.labelModel = new AntdUI.Label();
            this.labelInputTokens = new AntdUI.Label();
            this.labelOutputTokens = new AntdUI.Label();
            this.labelElapsed = new AntdUI.Label();
            this.labelAITip = new AntdUI.Label();
            this.richTextBoxResult = new System.Windows.Forms.RichTextBox();
            this.panelReasoning = new System.Windows.Forms.Panel();
            this.buttonReasoning = new AntdUI.Button();
            this.richTextBoxReasoning = new System.Windows.Forms.RichTextBox();
            this.panelTop.SuspendLayout();
            this.panelContent.SuspendLayout();
            this.panelLoading.SuspendLayout();
            this.panelError.SuspendLayout();
            this.panelResult.SuspendLayout();
            this.panelInfoTags.SuspendLayout();
            this.panelReasoning.SuspendLayout();
            this.SuspendLayout();
            //
            // panelTop
            //
            this.panelTop.Controls.Add(this.buttonBack);
            this.panelTop.Controls.Add(this.labelTitle);
            this.panelTop.Controls.Add(this.buttonCopy);
            this.panelTop.Controls.Add(this.buttonRetry);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(420, 48);
            this.panelTop.TabIndex = 0;
            //
            // buttonBack
            //
            this.buttonBack.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonBack.IconSvg = "ArrowLeftOutlined";
            this.buttonBack.Location = new System.Drawing.Point(8, 8);
            this.buttonBack.Name = "buttonBack";
            this.buttonBack.Size = new System.Drawing.Size(70, 32);
            this.buttonBack.TabIndex = 0;
            this.buttonBack.Text = "返回";
            this.buttonBack.Type = AntdUI.TTypeMini.Default;
            this.buttonBack.Click += new System.EventHandler(this.buttonBack_Click);
            //
            // labelTitle
            //
            this.labelTitle.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelTitle.Location = new System.Drawing.Point(120, 10);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(180, 28);
            this.labelTitle.TabIndex = 1;
            this.labelTitle.Text = "AI 日志分析";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // buttonCopy
            //
            this.buttonCopy.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonCopy.IconSvg = "CopyOutlined";
            this.buttonCopy.Location = new System.Drawing.Point(340, 8);
            this.buttonCopy.Name = "buttonCopy";
            this.buttonCopy.Size = new System.Drawing.Size(32, 32);
            this.buttonCopy.TabIndex = 2;
            this.buttonCopy.Text = "";
            this.buttonCopy.Visible = false;
            this.buttonCopy.Click += new System.EventHandler(this.buttonCopy_Click);
            //
            // buttonRetry
            //
            this.buttonRetry.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonRetry.IconSvg = "ReloadOutlined";
            this.buttonRetry.Location = new System.Drawing.Point(378, 8);
            this.buttonRetry.Name = "buttonRetry";
            this.buttonRetry.Size = new System.Drawing.Size(32, 32);
            this.buttonRetry.TabIndex = 3;
            this.buttonRetry.Text = "";
            this.buttonRetry.Visible = false;
            this.buttonRetry.Click += new System.EventHandler(this.buttonRetry_Click);
            //
            // panelContent
            //
            this.panelContent.Controls.Add(this.panelResult);
            this.panelContent.Controls.Add(this.panelError);
            this.panelContent.Controls.Add(this.panelLoading);
            this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContent.Location = new System.Drawing.Point(0, 48);
            this.panelContent.Name = "panelContent";
            this.panelContent.Size = new System.Drawing.Size(420, 552);
            this.panelContent.TabIndex = 1;
            //
            // panelLoading
            //
            this.panelLoading.Controls.Add(this.panelSpin);
            this.panelLoading.Controls.Add(this.labelLoading);
            this.panelLoading.Controls.Add(this.labelSource);
            this.panelLoading.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLoading.Location = new System.Drawing.Point(0, 0);
            this.panelLoading.Name = "panelLoading";
            this.panelLoading.Size = new System.Drawing.Size(420, 552);
            this.panelLoading.TabIndex = 0;
            //
            // panelSpin
            //
            this.panelSpin.Location = new System.Drawing.Point(190, 160);
            this.panelSpin.Name = "panelSpin";
            this.panelSpin.Size = new System.Drawing.Size(40, 40);
            this.panelSpin.TabIndex = 3;
            this.panelSpin.Paint += new System.Windows.Forms.PaintEventHandler(this.panelSpin_Paint);
            //
            // labelLoading
            //
            this.labelLoading.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelLoading.Location = new System.Drawing.Point(0, 210);
            this.labelLoading.Name = "labelLoading";
            this.labelLoading.Size = new System.Drawing.Size(420, 26);
            this.labelLoading.TabIndex = 0;
            this.labelLoading.Text = "AI 正在分析日志...";
            this.labelLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // spinTimer
            //
            this.spinTimer.Interval = 30;
            this.spinTimer.Tick += new System.EventHandler(this.spinTimer_Tick);
            //
            // labelSource
            //
            this.labelSource.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelSource.ForeColor = System.Drawing.Color.Gray;
            this.labelSource.Location = new System.Drawing.Point(0, 240);
            this.labelSource.Name = "labelSource";
            this.labelSource.Size = new System.Drawing.Size(420, 24);
            this.labelSource.TabIndex = 1;
            this.labelSource.Text = "";
            this.labelSource.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // panelError
            //
            this.panelError.Controls.Add(this.labelError);
            this.panelError.Controls.Add(this.buttonErrorRetry);
            this.panelError.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelError.Location = new System.Drawing.Point(0, 0);
            this.panelError.Name = "panelError";
            this.panelError.Size = new System.Drawing.Size(420, 552);
            this.panelError.TabIndex = 1;
            this.panelError.Visible = false;
            //
            // labelError
            //
            this.labelError.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelError.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(67)))), ((int)(((byte)(54)))));
            this.labelError.Location = new System.Drawing.Point(20, 180);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(380, 100);
            this.labelError.TabIndex = 0;
            this.labelError.Text = "";
            this.labelError.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // buttonErrorRetry
            //
            this.buttonErrorRetry.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonErrorRetry.Location = new System.Drawing.Point(160, 300);
            this.buttonErrorRetry.Name = "buttonErrorRetry";
            this.buttonErrorRetry.Size = new System.Drawing.Size(100, 36);
            this.buttonErrorRetry.TabIndex = 1;
            this.buttonErrorRetry.Text = "重试";
            this.buttonErrorRetry.Click += new System.EventHandler(this.buttonErrorRetry_Click);
            //
            // panelResult
            //
            this.panelResult.Controls.Add(this.richTextBoxResult);
            this.panelResult.Controls.Add(this.panelReasoning);
            this.panelResult.Controls.Add(this.panelInfoTags);
            this.panelResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelResult.Location = new System.Drawing.Point(0, 0);
            this.panelResult.Name = "panelResult";
            this.panelResult.Size = new System.Drawing.Size(420, 552);
            this.panelResult.TabIndex = 2;
            this.panelResult.Visible = false;
            //
            // panelInfoTags
            //
            this.panelInfoTags.AutoScroll = false;
            this.panelInfoTags.Controls.Add(this.labelModel);
            this.panelInfoTags.Controls.Add(this.labelInputTokens);
            this.panelInfoTags.Controls.Add(this.labelOutputTokens);
            this.panelInfoTags.Controls.Add(this.labelElapsed);
            this.panelInfoTags.Controls.Add(this.labelAITip);
            this.panelInfoTags.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelInfoTags.Location = new System.Drawing.Point(0, 0);
            this.panelInfoTags.Name = "panelInfoTags";
            this.panelInfoTags.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.panelInfoTags.Size = new System.Drawing.Size(420, 56);
            this.panelInfoTags.TabIndex = 0;
            this.panelInfoTags.WrapContents = true;
            //
            // labelModel
            //
            this.labelModel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelModel.Location = new System.Drawing.Point(10, 8);
            this.labelModel.Name = "labelModel";
            this.labelModel.Size = new System.Drawing.Size(150, 18);
            this.labelModel.TabIndex = 0;
            this.labelModel.Text = "";
            this.labelModel.Visible = false;
            //
            // labelInputTokens
            //
            this.labelInputTokens.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelInputTokens.Location = new System.Drawing.Point(168, 8);
            this.labelInputTokens.Name = "labelInputTokens";
            this.labelInputTokens.Size = new System.Drawing.Size(100, 18);
            this.labelInputTokens.TabIndex = 1;
            this.labelInputTokens.Text = "";
            this.labelInputTokens.Visible = false;
            //
            // labelOutputTokens
            //
            this.labelOutputTokens.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelOutputTokens.Location = new System.Drawing.Point(276, 8);
            this.labelOutputTokens.Name = "labelOutputTokens";
            this.labelOutputTokens.Size = new System.Drawing.Size(100, 18);
            this.labelOutputTokens.TabIndex = 2;
            this.labelOutputTokens.Text = "";
            this.labelOutputTokens.Visible = false;
            //
            // labelElapsed
            //
            this.labelElapsed.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelElapsed.Location = new System.Drawing.Point(10, 30);
            this.labelElapsed.Name = "labelElapsed";
            this.labelElapsed.Size = new System.Drawing.Size(100, 18);
            this.labelElapsed.TabIndex = 3;
            this.labelElapsed.Text = "";
            this.labelElapsed.Visible = false;
            //
            // labelAITip
            //
            this.labelAITip.Font = new System.Drawing.Font("微软雅黑", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelAITip.ForeColor = System.Drawing.Color.Gray;
            this.labelAITip.Location = new System.Drawing.Point(118, 30);
            this.labelAITip.Name = "labelAITip";
            this.labelAITip.Size = new System.Drawing.Size(120, 18);
            this.labelAITip.TabIndex = 4;
            this.labelAITip.Text = "AI 生成，仅供参考";
            //
            // richTextBoxResult
            //
            this.richTextBoxResult.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxResult.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxResult.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.richTextBoxResult.Location = new System.Drawing.Point(0, 56);
            this.richTextBoxResult.Name = "richTextBoxResult";
            this.richTextBoxResult.ReadOnly = true;
            this.richTextBoxResult.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextBoxResult.Size = new System.Drawing.Size(420, 496);
            this.richTextBoxResult.TabIndex = 1;
            this.richTextBoxResult.Text = "";
            //
            // panelReasoning
            //
            this.panelReasoning.Controls.Add(this.richTextBoxReasoning);
            this.panelReasoning.Controls.Add(this.buttonReasoning);
            this.panelReasoning.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelReasoning.Location = new System.Drawing.Point(0, 452);
            this.panelReasoning.Name = "panelReasoning";
            this.panelReasoning.Size = new System.Drawing.Size(420, 100);
            this.panelReasoning.TabIndex = 2;
            this.panelReasoning.Visible = false;
            //
            // buttonReasoning
            //
            this.buttonReasoning.Dock = System.Windows.Forms.DockStyle.Top;
            this.buttonReasoning.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonReasoning.IconSvg = "BulbOutlined";
            this.buttonReasoning.Location = new System.Drawing.Point(0, 0);
            this.buttonReasoning.Name = "buttonReasoning";
            this.buttonReasoning.Size = new System.Drawing.Size(420, 28);
            this.buttonReasoning.TabIndex = 0;
            this.buttonReasoning.Text = "思考过程 ▼";
            this.buttonReasoning.Type = AntdUI.TTypeMini.Default;
            this.buttonReasoning.Click += new System.EventHandler(this.buttonReasoning_Click);
            //
            // richTextBoxReasoning
            //
            this.richTextBoxReasoning.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.richTextBoxReasoning.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxReasoning.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxReasoning.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.richTextBoxReasoning.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.richTextBoxReasoning.Location = new System.Drawing.Point(0, 28);
            this.richTextBoxReasoning.Name = "richTextBoxReasoning";
            this.richTextBoxReasoning.ReadOnly = true;
            this.richTextBoxReasoning.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextBoxReasoning.Size = new System.Drawing.Size(420, 72);
            this.richTextBoxReasoning.TabIndex = 1;
            this.richTextBoxReasoning.Text = "";
            this.richTextBoxReasoning.Visible = false;
            //
            // PanelAiAnalyze
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelTop);
            this.Name = "PanelAiAnalyze";
            this.Size = new System.Drawing.Size(420, 600);
            this.panelTop.ResumeLayout(false);
            this.panelContent.ResumeLayout(false);
            this.panelLoading.ResumeLayout(false);
            this.panelError.ResumeLayout(false);
            this.panelResult.ResumeLayout(false);
            this.panelInfoTags.ResumeLayout(false);
            this.panelReasoning.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private AntdUI.Button buttonBack;
        private AntdUI.Label labelTitle;
        private AntdUI.Button buttonCopy;
        private AntdUI.Button buttonRetry;
        private System.Windows.Forms.Panel panelContent;
        private System.Windows.Forms.Panel panelLoading;
        private AntdUI.Label labelLoading;
        private AntdUI.Label labelSource;
        private System.Windows.Forms.Panel panelSpin;
        private System.Windows.Forms.Timer spinTimer;
        private System.Windows.Forms.Panel panelError;
        private AntdUI.Label labelError;
        private AntdUI.Button buttonErrorRetry;
        private System.Windows.Forms.Panel panelResult;
        private System.Windows.Forms.FlowLayoutPanel panelInfoTags;
        private AntdUI.Label labelModel;
        private AntdUI.Label labelInputTokens;
        private AntdUI.Label labelOutputTokens;
        private AntdUI.Label labelElapsed;
        private AntdUI.Label labelAITip;
        private System.Windows.Forms.RichTextBox richTextBoxResult;
        private System.Windows.Forms.Panel panelReasoning;
        private AntdUI.Button buttonReasoning;
        private System.Windows.Forms.RichTextBox richTextBoxReasoning;
    }
}
