namespace MinecraftConnectTool
{
    partial class lanload
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
            this.label1 = new AntdUI.Label();
            this.lan1 = new MinecraftConnectTool.lan();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Silver;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(184)))), ((int)(((byte)(83)))), ((int)(((byte)(197)))), ((int)(((byte)(100)))));
            this.label1.Location = new System.Drawing.Point(424, 9);
            this.label1.Name = "label1";
            this.label1.PrefixSvg = "FilterOutlined";
            this.label1.Size = new System.Drawing.Size(103, 21);
            this.label1.TabIndex = 1;
            this.label1.TabStop = false;
            this.label1.Text = "已启用流量节省";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label1.Visible = false;
            // 
            // lan1
            // 
            this.lan1.BackColor = System.Drawing.Color.LightGray;
            this.lan1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lan1.Location = new System.Drawing.Point(0, 0);
            this.lan1.Name = "lan1";
            this.lan1.Size = new System.Drawing.Size(762, 36);
            this.lan1.TabIndex = 0;
            this.lan1.Text = "lan1";
            this.lan1.Click += new System.EventHandler(this.lan1_Click);
            // 
            // lanload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lan1);
            this.Name = "lanload";
            this.Size = new System.Drawing.Size(762, 36);
            this.ResumeLayout(false);

        }

        #endregion

        private lan lan1;
        private AntdUI.Label label1;
    }
}
