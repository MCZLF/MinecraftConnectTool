namespace MinecraftConnectTool
{
    partial class Form1
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

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            AntdUI.MenuItem menuItem1 = new AntdUI.MenuItem();
            AntdUI.MenuItem menuItem2 = new AntdUI.MenuItem();
            AntdUI.MenuItem menuItem3 = new AntdUI.MenuItem();
            AntdUI.MenuItem menuItem4 = new AntdUI.MenuItem();
            AntdUI.MenuItem menuItem5 = new AntdUI.MenuItem();
            AntdUI.MenuItem menuItem6 = new AntdUI.MenuItem();
            AntdUI.MenuItem menuItem7 = new AntdUI.MenuItem();
            AntdUI.MenuItem menuItem8 = new AntdUI.MenuItem();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.menu1 = new AntdUI.Menu();
            this.panel = new AntdUI.Panel();
            this.panelbar = new AntdUI.Panel();
            this.PageHeader = new AntdUI.PageHeader();
            this.input_search = new AntdUI.Input();
            this.button_mclogs = new AntdUI.Button();
            this.button1 = new AntdUI.Button();
            this.button_color = new AntdUI.Button();
            this.buttonSZ = new AntdUI.Button();
            this.PageHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // menu1
            // 
            this.menu1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.menu1.BackHover = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.menu1.Dock = System.Windows.Forms.DockStyle.Left;
            this.menu1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.menu1.IconRatio = 1.3F;
            this.menu1.Indent = true;
            menuItem1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            menuItem1.IconSvg = "CodeSandboxCircleFilled";
            menuItem2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            menuItem2.Tag = "P2PControl";
            menuItem2.Text = "主页";
            menuItem3.IconSvg = "";
            menuItem3.Tag = "P2PQA";
            menuItem3.Text = "常见问题";
            menuItem4.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            menuItem4.Tag = "P2PLog";
            menuItem4.Text = "运行日志";
            menuItem1.Sub.Add(menuItem2);
            menuItem1.Sub.Add(menuItem3);
            menuItem1.Sub.Add(menuItem4);
            menuItem1.Tag = "P2PHome";
            menuItem1.Text = "P2P模式";
            menuItem5.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            menuItem5.IconSvg = "CloseCircleFilled";
            menuItem5.Tag = "ZTHome";
            menuItem5.Text = "ETLobby(维护)";
            menuItem6.IconSvg = "BuildFilled";
            menuItem6.Tag = "Extension";
            menuItem6.Text = "拓展";
            menuItem6.Visible = false;
            menuItem7.IconActiveSvg = "";
            menuItem7.IconSvg = "CompassFilled";
            menuItem7.Tag = "Update";
            menuItem7.Text = "检查更新";
            menuItem8.IconSvg = "SettingFilled";
            menuItem8.Tag = "Settings";
            menuItem8.Text = "设置";
            this.menu1.Items.Add(menuItem1);
            this.menu1.Items.Add(menuItem5);
            this.menu1.Items.Add(menuItem6);
            this.menu1.Items.Add(menuItem7);
            this.menu1.Items.Add(menuItem8);
            this.menu1.Location = new System.Drawing.Point(0, 40);
            this.menu1.Name = "menu1";
            this.menu1.Size = new System.Drawing.Size(140, 504);
            this.menu1.TabIndex = 5;
            this.menu1.Text = "menu1";
            this.menu1.SelectChanged += new AntdUI.SelectEventHandler(this.menu1_SelectChanged_1);
            // 
            // panel
            // 
            this.panel.Location = new System.Drawing.Point(139, 48);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(762, 455);
            this.panel.TabIndex = 6;
            this.panel.Text = "panel1";
            this.panel.Click += new System.EventHandler(this.panel_Click);
            // 
            // panelbar
            // 
            this.panelbar.Location = new System.Drawing.Point(139, 507);
            this.panelbar.Name = "panelbar";
            this.panelbar.Size = new System.Drawing.Size(762, 36);
            this.panelbar.TabIndex = 8;
            this.panelbar.Text = "bar";
            // 
            // PageHeader
            // 
            this.PageHeader.Controls.Add(this.input_search);
            this.PageHeader.Controls.Add(this.button_mclogs);
            this.PageHeader.Controls.Add(this.button1);
            this.PageHeader.Controls.Add(this.button_color);
            this.PageHeader.Controls.Add(this.buttonSZ);
            this.PageHeader.DividerShow = true;
            this.PageHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.PageHeader.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.PageHeader.Location = new System.Drawing.Point(0, 0);
            this.PageHeader.MaximizeBox = false;
            this.PageHeader.Name = "PageHeader";
            this.PageHeader.ShowButton = true;
            this.PageHeader.ShowIcon = true;
            this.PageHeader.Size = new System.Drawing.Size(901, 40);
            this.PageHeader.SubText = "(版本获取失败)";
            this.PageHeader.TabIndex = 1;
            this.PageHeader.Text = "MinecraftConnectTool";
            this.PageHeader.Click += new System.EventHandler(this.PageHeader_Click);
            // 
            // input_search
            // 
            this.input_search.AllowClear = true;
            this.input_search.Dock = System.Windows.Forms.DockStyle.Right;
            this.input_search.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.input_search.LocalizationPlaceholderText = "search";
            this.input_search.Location = new System.Drawing.Point(405, 0);
            this.input_search.Name = "input_search";
            this.input_search.PlaceholderText = "搜索";
            this.input_search.PrefixSvg = "SearchOutlined";
            this.input_search.Size = new System.Drawing.Size(200, 40);
            this.input_search.TabIndex = 8;
            // 
            // button_mclogs
            // 
            this.button_mclogs.Dock = System.Windows.Forms.DockStyle.Right;
            this.button_mclogs.Ghost = true;
            this.button_mclogs.IconRatio = 0.65F;
            this.button_mclogs.IconSvg = "CloudUploadOutlined";
            this.button_mclogs.Location = new System.Drawing.Point(605, 0);
            this.button_mclogs.Name = "button_mclogs";
            this.button_mclogs.Radius = 0;
            this.button_mclogs.Size = new System.Drawing.Size(50, 40);
            this.button_mclogs.TabIndex = 7;
            this.button_mclogs.WaveSize = 0;
            this.button_mclogs.Click += new System.EventHandler(this.button_mclogs_Click);
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Right;
            this.button1.Ghost = true;
            this.button1.IconRatio = 0.6F;
            this.button1.IconSvg = "ReloadOutlined";
            this.button1.Location = new System.Drawing.Point(655, 0);
            this.button1.Name = "button1";
            this.button1.Radius = 0;
            this.button1.Size = new System.Drawing.Size(50, 40);
            this.button1.TabIndex = 5;
            this.button1.WaveSize = 0;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button_color
            // 
            this.button_color.Dock = System.Windows.Forms.DockStyle.Right;
            this.button_color.Ghost = true;
            this.button_color.IconRatio = 0.6F;
            this.button_color.IconSvg = "QuestionCircleOutlined";
            this.button_color.Location = new System.Drawing.Point(705, 0);
            this.button_color.Name = "button_color";
            this.button_color.Radius = 0;
            this.button_color.Size = new System.Drawing.Size(50, 40);
            this.button_color.TabIndex = 1;
            this.button_color.ToggleIconSvg = "RedoOutlined";
            this.button_color.WaveSize = 0;
            this.button_color.Click += new System.EventHandler(this.button_color_Click);
            // 
            // buttonSZ
            // 
            this.buttonSZ.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonSZ.Ghost = true;
            this.buttonSZ.IconSvg = resources.GetString("buttonSZ.IconSvg");
            this.buttonSZ.Location = new System.Drawing.Point(755, 0);
            this.buttonSZ.Name = "buttonSZ";
            this.buttonSZ.Radius = 0;
            this.buttonSZ.Size = new System.Drawing.Size(50, 40);
            this.buttonSZ.TabIndex = 0;
            this.buttonSZ.WaveSize = 0;
            this.buttonSZ.Click += new System.EventHandler(this.buttonSZ_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(901, 544);
            this.Controls.Add(this.menu1);
            this.Controls.Add(this.PageHeader);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.panelbar);
            this.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MinecraftConnectTool";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.PageHeader.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private AntdUI.Button button_color;
        private AntdUI.Button buttonSZ;
        private AntdUI.Menu menu1;
        public AntdUI.PageHeader PageHeader;
        private AntdUI.Panel panelbar;
        private AntdUI.Panel panel;
        private AntdUI.Button button1;
        private AntdUI.Input input_search;
        private AntdUI.Button button_mclogs;
    }
}

