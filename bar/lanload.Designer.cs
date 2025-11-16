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
            this.lan1 = new MinecraftConnectTool.lan();
            this.SuspendLayout();
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
            // 
            // lanload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lan1);
            this.Name = "lanload";
            this.Size = new System.Drawing.Size(762, 36);
            this.ResumeLayout(false);

        }

        #endregion

        private lan lan1;
    }
}
