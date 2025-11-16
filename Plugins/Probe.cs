// Probe.cs  (.NET 4.7.2 WinForm 客户端插件)
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftConnectTool
{
    internal static class Probe
    {
        //================ 可改常量 ================
        private const string HOST = "mctservice.mczlf.loft.games";
        private const int PORT = 17600;
        private const int TIMEOUT = 5000;

        //================ 开关 ================
        private static bool _enablePopup = false;   // false=完全静默
        public static bool EnablePopup
        {
            get => _enablePopup;
            set => _enablePopup = value;
        }

        //================ 版本号 ================
        public static string Version = Form1.version; 

        //================ 发送接口 ================
        public static async Task SendAsync()
        {
            await Task.Run(async () =>
            {
                string fail = null;
                try
                {
                    // 1. 拼协议
                    string body =
$@"====ProbeContext====
Version = {Version}
Time = {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";
                    byte[] data = Encoding.UTF8.GetBytes(body);

                    // 2. TCP
                    using (TcpClient c = new TcpClient())
                    {
                        var t = c.ConnectAsync(HOST, PORT);
                        bool ok = await Task.WhenAny(t, Task.Delay(TIMEOUT)) == t && t.IsCompleted;
                        if (!ok) { fail = "连接超时"; return; }

                        var s = c.GetStream();
                        s.WriteTimeout = TIMEOUT;
                        s.ReadTimeout = TIMEOUT;

                        await s.WriteAsync(data, 0, data.Length);

                        // 3. 回包
                        byte[] buf = new byte[256];
                        int n = await s.ReadAsync(buf, 0, buf.Length);
                        string r = Encoding.UTF8.GetString(buf, 0, n).Trim();

                        if (r.StartsWith("LIMIT", StringComparison.OrdinalIgnoreCase))
                            fail = "本小时已达上限";
                        else if (r.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                            fail = null;
                        else
                            fail = "未知返回";
                    }
                }
                catch { fail = "网络异常"; }
                finally
                {
                    if (_enablePopup && !string.IsNullOrEmpty(fail))
                        System.Windows.Forms.MessageBox.Show(fail, "Probe", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
            });
        }
    }
}