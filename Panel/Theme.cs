using AntdUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class Theme : UserControl
    {
        public Theme()
        {
            InitializeComponent();
            uploadDragger1.ClickHand = false;

            // 拖拽
            uploadDragger1.DragChanged += (s, e) =>
            {
                if (e.Value?.Length > 0) SaveBgImage(e.Value[0]);
            };

            // 点击
            uploadDragger1.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "图片|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.webp";
                    dlg.Multiselect = false;
                    if (dlg.ShowDialog() == DialogResult.OK)
                        SaveBgImage(dlg.FileName);
                }
            };
        }

        void SaveBgImage(string path)
        {
            var dr = MessageBox.Show(
        "当前版本的照片背景非常拉跨，效果非常差，是否继续？",
        "警告",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning);

            if (dr == DialogResult.No) return;
            var ext = Path.GetExtension(path).ToLower();
            if (Array.IndexOf(new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" }, ext) < 0)
                return;
            ThemeConfig.Write("BackPNG", "True");
            ThemeConfig.Write("BackPNGAdd", path);
            //Program.alertwarn($"DEBUG:{path}");
            //GETPATHSuccess
        }

        private void colorPicker1_ValueChanged(object sender, AntdUI.ColorEventArgs e)
        {

        }
        static class ShowConfig
        {
            public static Color Read(string key)
            {
                var hex = ThemeConfig.ReadHex(key);
                return hex == null ? default : ColorTranslator.FromHtml(hex);
            }
        }
        private void Theme_Load(object sender, EventArgs e)
        {
            bool enableColor = Form1.config.read<bool>("EnableColor", false);
            switch2.Checked = enableColor;          // 先设状态
            switch2.CheckedChanged += (s, q) =>     // 再挂事件
            {
                foreach (Control c in switch2.Parent.Controls)
                    c.Enabled = (c == switch2) || switch2.Checked;
            };
            foreach (Control c in switch2.Parent.Controls)
                c.Enabled = (c == switch2) || enableColor;
            label14.Visible = true;
            colorPicker1.Value = ShowConfig.Read("Title");
            colorPicker2.Value = ShowConfig.Read("LeftMenu");
            colorPicker3.Value = ShowConfig.Read("LeftMenuHover");
            colorPicker4.Value = ShowConfig.Read("P2PBack");
            colorPicker5.Value = ShowConfig.Read("P2PButton");
            colorPicker6.Value = ShowConfig.Read("P2PMTWrite");
            colorPicker7.Value = ShowConfig.Read("P2PLogBack");
            colorPicker8.Value = ShowConfig.Read("LogPanelBack");
            colorPicker9.Value = ShowConfig.Read("SettingButtonColor");
            colorPicker10.Value = ShowConfig.Read("SettingBack");
            colorPicker12.Value = ShowConfig.Read("UpdateButton");
            colorPicker11.Value = ShowConfig.Read("CandyButton");
            colorPicker13.Value = ShowConfig.Read("CandyInput");
            bool EnableATDDark = Form1.config.read<bool>("EnableATDDark", false);
            if (EnableATDDark)
            {
                switch6.Checked = true;
            }
        }

        private void colorPicker2_ValueChanged(object sender, ColorEventArgs e)
        {

        }

        private void colorPicker3_ValueChanged(object sender, ColorEventArgs e)
        {

        }

        private void save_Click(object sender, EventArgs e)
        {
            ThemeConfig.Write("Title", colorPicker1.Value.ToHex());
            ThemeConfig.Write("LeftMenu", colorPicker2.Value.ToHex());
            ThemeConfig.Write("LeftMenuHover", colorPicker3.Value.ToHex());
            ThemeConfig.Write("P2PBack", colorPicker4.Value.ToHex());
            ThemeConfig.Write("P2PButton", colorPicker5.Value.ToHex());
            ThemeConfig.Write("P2PMTWrite", colorPicker6.Value.ToHex());
            ThemeConfig.Write("P2PLogBack", colorPicker7.Value.ToHex());
            ThemeConfig.Write("LogPanelBack", colorPicker8.Value.ToHex());
            ThemeConfig.Write("SettingButtonColor", colorPicker9.Value.ToHex());
            ThemeConfig.Write("SettingBack", colorPicker10.Value.ToHex());
            ThemeConfig.Write("UpdateButton", colorPicker12.Value.ToHex());
            ThemeConfig.Write("CandyButton", colorPicker11.Value.ToHex());
            ThemeConfig.Write("CandyInput", colorPicker13.Value.ToHex());
            Application.Restart();
        }

        private void colorPicker4_ValueChanged(object sender, ColorEventArgs e)
        {

        }

        private void colorPicker5_ValueChanged(object sender, ColorEventArgs e)
        {

        }

        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void switch1_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (switch1.Checked)
            {
                colorPicker4.Enabled = false;
                uploadDragger1.Visible = true;
                ThemeConfig.Write("BackPNG", "True");
                switch1.Checked = true;
            }
            else
            {
                colorPicker4.Enabled = true;
                switch1.Checked = false;
                ThemeConfig.Write("BackPNG", "False");
                ThemeConfig.Write("BackPNGAdd", "");
                uploadDragger1.Visible = false;
            }
        }

        private void switch2_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (switch2.Checked)
            {
                Form1.config.write("EnableColor", true);
                switch2.Checked = true;
                label14.Visible = true;
            }
            else
            {
                Form1.config.write("EnableColor", false);
                switch2.Checked = false;
                label14.Visible = true;
            }
        }

        private void switch6_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (switch6.Checked)
            {
                Form1.config.write("EnableATDDark", true);
                switch6.Checked = true;
            }
            else
            {
                Form1.config.write("EnableATDDark", false);
                switch6.Checked = false;
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "MCZLFAPP");
                Directory.CreateDirectory(dir);
                var json = Path.Combine(dir, "theme.json");

                if (!File.Exists(json)) File.WriteAllText(json, "{}", Encoding.UTF8);
                Process.Start("notepad.exe", json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开配置文件失败：\n" + ex.Message,
                               "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    static class ThemeConfig
    {
        private static readonly string path =
            Path.Combine(Path.GetTempPath(), "MCZLFAPP", "theme.json");

        public static void Write(string key, string value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var d = File.Exists(path)
                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path))
                : new Dictionary<string, string>();
            d[key] = value;
            File.WriteAllText(path, JsonConvert.SerializeObject(d, Formatting.Indented));
        }

        // 专门读颜色，自动补 #
        public static string ReadHex(string key)
        {
            if (!File.Exists(path)) return "#FFC0CB";
            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
            return d.TryGetValue(key, out var h) && !string.IsNullOrEmpty(h)
                   ? h.StartsWith("#") ? h : "#" + h
                   : "#FFC0CB";
        }

        // 专门读字符串，原样返回
        public static string ReadString(string key)
        {
            if (!File.Exists(path)) return null;
            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
            return d.TryGetValue(key, out var v) ? v : null;
        }

        // 专门读布尔
        public static bool ReadBool(string key)
        {
            var v = ReadString(key);
            return bool.TryParse(v, out var b) && b;
        }




        // 读取是否启用背景图
        public static bool BackPngEnabled =>
            "True".Equals(ThemeConfig.ReadHex("BackPNG"), StringComparison.OrdinalIgnoreCase);

        // 读取背景图路径
        public static string BackPngPath =>
            ThemeConfig.ReadHex("BackPNGAdd") ?? string.Empty;
    }
}