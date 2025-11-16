namespace MinecraftConnectTool
{
    partial class P2PQNA
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(P2PQNA));
            this.collapse1 = new AntdUI.Collapse();
            this.collapseItem5 = new AntdUI.CollapseItem();
            this.alert1 = new AntdUI.Alert();
            this.label1 = new AntdUI.Label();
            this.collapseItem1 = new AntdUI.CollapseItem();
            this.alert2 = new AntdUI.Alert();
            this.label2 = new AntdUI.Label();
            this.collapseItem2 = new AntdUI.CollapseItem();
            this.alert4 = new AntdUI.Alert();
            this.collapseItem3 = new AntdUI.CollapseItem();
            this.alert5 = new AntdUI.Alert();
            this.alert3 = new AntdUI.Alert();
            this.collapseItem4 = new AntdUI.CollapseItem();
            this.label3 = new AntdUI.Label();
            this.collapseItem6 = new AntdUI.CollapseItem();
            this.label4 = new AntdUI.Label();
            this.collapseItem8 = new AntdUI.CollapseItem();
            this.label5 = new AntdUI.Label();
            this.collapseItem9 = new AntdUI.CollapseItem();
            this.alert6 = new AntdUI.Alert();
            this.collapseItem7 = new AntdUI.CollapseItem();
            this.alert8 = new AntdUI.Alert();
            this.alert7 = new AntdUI.Alert();
            this.collapse1.SuspendLayout();
            this.collapseItem5.SuspendLayout();
            this.collapseItem1.SuspendLayout();
            this.collapseItem2.SuspendLayout();
            this.collapseItem3.SuspendLayout();
            this.collapseItem4.SuspendLayout();
            this.collapseItem6.SuspendLayout();
            this.collapseItem8.SuspendLayout();
            this.collapseItem9.SuspendLayout();
            this.collapseItem7.SuspendLayout();
            this.SuspendLayout();
            // 
            // collapse1
            // 
            this.collapse1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.collapse1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.collapse1.Items.Add(this.collapseItem5);
            this.collapse1.Items.Add(this.collapseItem1);
            this.collapse1.Items.Add(this.collapseItem2);
            this.collapse1.Items.Add(this.collapseItem3);
            this.collapse1.Items.Add(this.collapseItem4);
            this.collapse1.Items.Add(this.collapseItem6);
            this.collapse1.Items.Add(this.collapseItem8);
            this.collapse1.Items.Add(this.collapseItem9);
            this.collapse1.Items.Add(this.collapseItem7);
            this.collapse1.Location = new System.Drawing.Point(0, 0);
            this.collapse1.Name = "collapse1";
            this.collapse1.Size = new System.Drawing.Size(762, 455);
            this.collapse1.TabIndex = 48;
            this.collapse1.Text = "collapse1";
            this.collapse1.Unique = true;
            this.collapse1.ExpandChanged += new AntdUI.CollapseExpandEventHandler(this.collapse1_ExpandChanged);
            // 
            // collapseItem5
            // 
            this.collapseItem5.Controls.Add(this.alert1);
            this.collapseItem5.Controls.Add(this.label1);
            this.collapseItem5.Location = new System.Drawing.Point(-724, -41);
            this.collapseItem5.Name = "collapseItem5";
            this.collapseItem5.Size = new System.Drawing.Size(724, 41);
            this.collapseItem5.TabIndex = 4;
            this.collapseItem5.Text = "Connection refused: getsockopt";
            // 
            // alert1
            // 
            this.alert1.Icon = AntdUI.TType.Info;
            this.alert1.Location = new System.Drawing.Point(0, 14);
            this.alert1.Name = "alert1";
            this.alert1.Radius = 20;
            this.alert1.Size = new System.Drawing.Size(724, 26);
            this.alert1.TabIndex = 1;
            this.alert1.Text = "长时间提示getsockopt的话，请检查网络是否正常";
            this.alert1.Click += new System.EventHandler(this.alert1_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(0, -8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(724, 33);
            this.label1.SuffixSvg = "ClockCircleFilled";
            this.label1.TabIndex = 0;
            this.label1.Text = "P2P正在打洞,请稍等片刻 过一会儿就好了";
            // 
            // collapseItem1
            // 
            this.collapseItem1.Controls.Add(this.alert2);
            this.collapseItem1.Controls.Add(this.label2);
            this.collapseItem1.Location = new System.Drawing.Point(-724, -118);
            this.collapseItem1.Name = "collapseItem1";
            this.collapseItem1.Size = new System.Drawing.Size(724, 118);
            this.collapseItem1.TabIndex = 0;
            this.collapseItem1.Text = "提示No further information";
            // 
            // alert2
            // 
            this.alert2.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.alert2.Icon = AntdUI.TType.Warn;
            this.alert2.Location = new System.Drawing.Point(6, 63);
            this.alert2.Name = "alert2";
            this.alert2.Size = new System.Drawing.Size(715, 52);
            this.alert2.TabIndex = 1;
            this.alert2.Text = "请检查房主提供的[提示码]和[端口是否正确]，或者玩家是否输入正确  （默认邀请信息的那一段中文不要加!）";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(8, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(719, 59);
            this.label2.TabIndex = 0;
            this.label2.Text = "如果你是刚加入房间 就来游戏加入 那可能是核心还没加载\r\n如果已经开了一会儿了，那么↓\r\n或是防火墙高级设置中添加规则，进站规则和出站规则中TCP连接规则加入对应" +
    "端口或全开放";
            // 
            // collapseItem2
            // 
            this.collapseItem2.Controls.Add(this.alert4);
            this.collapseItem2.Location = new System.Drawing.Point(-724, -43);
            this.collapseItem2.Name = "collapseItem2";
            this.collapseItem2.Size = new System.Drawing.Size(724, 43);
            this.collapseItem2.TabIndex = 1;
            this.collapseItem2.Text = "登入失败，无效的会话";
            // 
            // alert4
            // 
            this.alert4.Icon = AntdUI.TType.Info;
            this.alert4.Location = new System.Drawing.Point(4, 2);
            this.alert4.Name = "alert4";
            this.alert4.Size = new System.Drawing.Size(719, 40);
            this.alert4.TabIndex = 0;
            this.alert4.Text = "1.加入方加入Mod-自定义联机 在开启房间时 关闭在线模式(这样离线也能加)      2.使用外置登录(例如littleskin)     3.正版账号";
            // 
            // collapseItem3
            // 
            this.collapseItem3.Controls.Add(this.alert5);
            this.collapseItem3.Controls.Add(this.alert3);
            this.collapseItem3.Location = new System.Drawing.Point(-724, -38);
            this.collapseItem3.Name = "collapseItem3";
            this.collapseItem3.Size = new System.Drawing.Size(724, 38);
            this.collapseItem3.TabIndex = 2;
            this.collapseItem3.Text = "未知主机";
            // 
            // alert5
            // 
            this.alert5.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.alert5.Icon = AntdUI.TType.Info;
            this.alert5.Location = new System.Drawing.Point(390, 0);
            this.alert5.Name = "alert5";
            this.alert5.Size = new System.Drawing.Size(333, 37);
            this.alert5.TabIndex = 1;
            this.alert5.Text = "不是会自动复制吗。。为什么要手动输入";
            // 
            // alert3
            // 
            this.alert3.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.alert3.Icon = AntdUI.TType.Error;
            this.alert3.Location = new System.Drawing.Point(0, 0);
            this.alert3.Name = "alert3";
            this.alert3.Size = new System.Drawing.Size(399, 35);
            this.alert3.TabIndex = 0;
            this.alert3.Text = "ip:端口 中，请勿使用中文冒号,请使用 英文冒号";
            // 
            // collapseItem4
            // 
            this.collapseItem4.Controls.Add(this.label3);
            this.collapseItem4.Location = new System.Drawing.Point(-724, -60);
            this.collapseItem4.Name = "collapseItem4";
            this.collapseItem4.Size = new System.Drawing.Size(724, 60);
            this.collapseItem4.TabIndex = 3;
            this.collapseItem4.Text = "Invalid characters in username";
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(0, 4);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(724, 53);
            this.label3.TabIndex = 0;
            this.label3.Text = "- 【大部分都是这个】用户名长度过长，正常用户名长度应该不超过16位，并且不应该出现中文\r\n- 某个mod存在漏洞，这个报错属于编程错误，请查看游戏日志，检查玩家" +
    "进入时的报错，移除相关mod\r\n- 数据包过大";
            // 
            // collapseItem6
            // 
            this.collapseItem6.Controls.Add(this.label4);
            this.collapseItem6.Location = new System.Drawing.Point(-724, -60);
            this.collapseItem6.Name = "collapseItem6";
            this.collapseItem6.Size = new System.Drawing.Size(724, 60);
            this.collapseItem6.TabIndex = 5;
            this.collapseItem6.Text = "连接中断/连接超时 进入后一直掉线（或者加载不出地形/掉虚空 等等类似的描述）";
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(719, 57);
            this.label4.TabIndex = 0;
            this.label4.Text = "进入后一直掉线（或者加载不出地形/掉虚空 等等类似的描述）\r\n尝试房主把渲染距离和模拟距离调到最低，加入方进入后稳定后再调回来\r\n或者使用预览版本 （P2P核心优" +
    "化）";
            // 
            // collapseItem8
            // 
            this.collapseItem8.Controls.Add(this.label5);
            this.collapseItem8.Location = new System.Drawing.Point(-724, -60);
            this.collapseItem8.Name = "collapseItem8";
            this.collapseItem8.Size = new System.Drawing.Size(724, 60);
            this.collapseItem8.TabIndex = 7;
            this.collapseItem8.Text = "Tried to read NBT tag that was too big; tried to allocate: Xbytes where max allow" +
    "ed: 2097152";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(0, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(723, 60);
            this.label5.TabIndex = 0;
            this.label5.Text = resources.GetString("label5.Text");
            this.label5.Click += new System.EventHandler(this.label5_Click);
            // 
            // collapseItem9
            // 
            this.collapseItem9.Controls.Add(this.alert6);
            this.collapseItem9.Location = new System.Drawing.Point(-724, -30);
            this.collapseItem9.Name = "collapseItem9";
            this.collapseItem9.Size = new System.Drawing.Size(724, 30);
            this.collapseItem9.TabIndex = 8;
            this.collapseItem9.Text = "Badly compressed packet - size of X is larger than protocol maximum of 2097152";
            // 
            // alert6
            // 
            this.alert6.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.alert6.Icon = AntdUI.TType.Info;
            this.alert6.Location = new System.Drawing.Point(3, 1);
            this.alert6.Name = "alert6";
            this.alert6.Size = new System.Drawing.Size(720, 29);
            this.alert6.TabIndex = 0;
            this.alert6.Text = "同上一个（Tried to read NBT tag that was too big; tried to allocate: Xbytes where max " +
    "allowed: 2097152）";
            // 
            // collapseItem7
            // 
            this.collapseItem7.Controls.Add(this.alert8);
            this.collapseItem7.Controls.Add(this.alert7);
            this.collapseItem7.Expand = true;
            this.collapseItem7.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.collapseItem7.Full = true;
            this.collapseItem7.Location = new System.Drawing.Point(19, 388);
            this.collapseItem7.Name = "collapseItem7";
            this.collapseItem7.Size = new System.Drawing.Size(724, 48);
            this.collapseItem7.TabIndex = 9;
            this.collapseItem7.Text = "没有我遇到的问题";
            // 
            // alert8
            // 
            this.alert8.Icon = AntdUI.TType.Info;
            this.alert8.Location = new System.Drawing.Point(356, 0);
            this.alert8.Name = "alert8";
            this.alert8.Size = new System.Drawing.Size(367, 47);
            this.alert8.TabIndex = 1;
            this.alert8.Text = "查看帮助文档 https://mct.mczlf.loft.games";
            this.alert8.Click += new System.EventHandler(this.alert8_Click);
            // 
            // alert7
            // 
            this.alert7.Icon = AntdUI.TType.Success;
            this.alert7.Location = new System.Drawing.Point(2, 1);
            this.alert7.Name = "alert7";
            this.alert7.Size = new System.Drawing.Size(348, 46);
            this.alert7.TabIndex = 0;
            this.alert7.Text = "加入QQ群获取帮助 690625244";
            this.alert7.Click += new System.EventHandler(this.alert7_Click);
            // 
            // P2PQNA
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.collapse1);
            this.Name = "P2PQNA";
            this.Size = new System.Drawing.Size(762, 455);
            this.collapse1.ResumeLayout(false);
            this.collapseItem5.ResumeLayout(false);
            this.collapseItem1.ResumeLayout(false);
            this.collapseItem2.ResumeLayout(false);
            this.collapseItem3.ResumeLayout(false);
            this.collapseItem4.ResumeLayout(false);
            this.collapseItem6.ResumeLayout(false);
            this.collapseItem8.ResumeLayout(false);
            this.collapseItem9.ResumeLayout(false);
            this.collapseItem7.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Collapse collapse1;
        private AntdUI.CollapseItem collapseItem1;
        private AntdUI.CollapseItem collapseItem2;
        private AntdUI.CollapseItem collapseItem3;
        private AntdUI.CollapseItem collapseItem4;
        private AntdUI.CollapseItem collapseItem6;
        private AntdUI.CollapseItem collapseItem5;
        private AntdUI.Alert alert1;
        private AntdUI.Label label1;
        private AntdUI.CollapseItem collapseItem8;
        private AntdUI.CollapseItem collapseItem9;
        private AntdUI.Alert alert2;
        private AntdUI.Label label2;
        private AntdUI.Alert alert4;
        private AntdUI.Alert alert5;
        private AntdUI.Alert alert3;
        private AntdUI.Label label3;
        private AntdUI.Label label4;
        private AntdUI.Label label5;
        private AntdUI.Alert alert6;
        private AntdUI.CollapseItem collapseItem7;
        private AntdUI.Alert alert8;
        private AntdUI.Alert alert7;
    }
}
