using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        public static Form1 MainForm { get; private set; }
        public static Font AlertFont { get; } = new Font("Microsoft YaHei UI", 8.3f);

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            if (System.IO.File.Exists("debug"))
            {
                InitDebugConsole();
            }
            bool isElevated = args.Contains("--admin");
            int admin = 0;
            if (!isElevated)
            {
                // 尝试以管理员权限重新启动
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Application.ExecutablePath,
                    Arguments = "--admin",
                    Verb = "runas" // 请求管理员权限
                };

                try
                {
                    using (var proc = System.Diagnostics.Process.Start(startInfo))
                    {
                        return; // 启动新进程后退出当前进程
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // 用户拒绝UAC，继续以普通权限运行
                    admin = 2;
                }
            }
            else
            {
                admin = 1;
            }
            // 设置全局变量（你可以根据需要改为静态字段或其他方式）
            Program.admin = admin;;
            MainForm = new Form1();
            Application.Run(MainForm);
        }

        // 全局方法
        public static int admin = 0;
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, $"发生了一个意料之外的错误：\n{e.Exception.Message}", "详细信息：\n" + e.Exception.StackTrace, AntdUI.TType.Error)
            {
                CloseIcon = true,
                Font = AlertFont,
                Draggable = false,
                CancelText = null,
                OkText = "好的"
            });
        }
        public static void alerterror(string message)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, "Error\n发生了意料之外的错误", message, AntdUI.TType.Error)
            {
                CloseIcon = true,
                Font = Program.AlertFont, // 可以根据需要调整字体
                Draggable = false,
                CancelText = null,
                OkText = "知道了"
            });
        }
        public static void alertwarn(string message)
        {
            AntdUI.Modal.open(new AntdUI.Modal.Config(Program.MainForm, "Warn\n警告(＃°Д°)", message, AntdUI.TType.Warn)
            {
                CloseIcon = true,
                Font = Program.AlertFont, // 可以根据需要调整字体
                Draggable = false,
                CancelText = null,
                OkText = "知道了"
            });
        }
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool SetConsoleOutputCP(uint wCodePageID);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool SetConsoleCP(uint wCodePageID);

        static void InitDebugConsole()
        {
            AllocConsole();
            SetConsoleOutputCP(65001);
            SetConsoleCP(65001);
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 接管输出流以实现自动着色
            System.Console.SetOut(new ColorInterceptor(System.Console.Out));
            System.Console.SetError(new ColorInterceptor(System.Console.Error));
        }

        class ColorInterceptor : System.IO.TextWriter
        {
            private System.IO.TextWriter _inner;

            public ColorInterceptor(System.IO.TextWriter inner) => _inner = inner;

            public override System.Text.Encoding Encoding => _inner.Encoding;

            public override void Write(string value)
            {
                if (value == null) return;

                var origin = System.Console.ForegroundColor;

                // 无额外字符串分配，忽略大小写检测关键词
                if (value.IndexOf("error", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                else if (value.IndexOf("warn", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    System.Console.ForegroundColor = System.ConsoleColor.DarkYellow; // 橙色
                else if (value.IndexOf("success", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    System.Console.ForegroundColor = System.ConsoleColor.Green;

                _inner.Write(value);
                System.Console.ForegroundColor = origin;
            }

            public override void WriteLine(string value)
            {
                _inner.Write(DateTime.Now.ToString("[HH:mm:ss.fff] "));
                Write(value);
                _inner.WriteLine();
            }
        }
    }
}