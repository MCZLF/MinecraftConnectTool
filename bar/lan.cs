using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class lan : Control
    {
        private const int CONTROL_WIDTH = 762;
        private const int CONTROL_HEIGHT = 36;
        private Label uploadLabel;
        private Label downloadLabel;
        private Label disclaimerLabel;
        private Timer updateTimer;
        private long lastUploadBytes;
        private long lastDownloadBytes;
        private NetworkInterface networkInterface;

        public lan()
        {
            this.Width = CONTROL_WIDTH;
            this.Height = CONTROL_HEIGHT;
            this.BackColor = Color.LightGray;
            uploadLabel = new Label
            {
                Text = "Upload/上传: 0 KB/s",
                Location = new Point(10, 5),
                Width = 200,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 12)
            };
            this.Controls.Add(uploadLabel);

            downloadLabel = new Label
            {
                Text = "Download/下载: 0 KB/s",
                Location = new Point(220, 5),
                Width = 200,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 12)
            };
            this.Controls.Add(downloadLabel);
            disclaimerLabel = new Label
            {
                Text = "读取自系统接口，不代表程序上传及下载",
                Location = new Point(CONTROL_WIDTH - 250, 5),
                Width = 240,
                Height = 30,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Arial", 8),
                ForeColor = Color.Silver
            };
            this.Controls.Add(disclaimerLabel);

            // 初始化定时器
            updateTimer = new Timer
            {
                Interval = 1000 // 每秒更新一次
            };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            // 初始化网络接口
            networkInterface = GetActiveNetworkInterface();
            if (networkInterface != null)
            {
                lastUploadBytes = networkInterface.GetIPv4Statistics().BytesSent;
                lastDownloadBytes = networkInterface.GetIPv4Statistics().BytesReceived;
            }
        }

        private NetworkInterface GetActiveNetworkInterface()
        {
            // 获取当前活动的网络接口
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    return ni;
                }
            }
            return null;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (networkInterface != null)
            {
                var stats = networkInterface.GetIPv4Statistics();
                long currentUploadBytes = stats.BytesSent;
                long currentDownloadBytes = stats.BytesReceived;

                // 计算上传和下载速率
                long uploadBytesPerSecond = currentUploadBytes - lastUploadBytes;
                long downloadBytesPerSecond = currentDownloadBytes - lastDownloadBytes;

                // 更新显示
                uploadLabel.Text = $"Upload: {uploadBytesPerSecond / 1024.0:0.0} KB/s";
                downloadLabel.Text = $"Download: {downloadBytesPerSecond / 1024.0:0.0} KB/s";

                // 更新上次的流量数据
                lastUploadBytes = currentUploadBytes;
                lastDownloadBytes = currentDownloadBytes;
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }
    }
}