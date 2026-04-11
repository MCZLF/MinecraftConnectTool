using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class update7 : UserControl
    {
        private readonly string downloadUrl32 = "https://api.mct.mczlf.loft.games/007/Win_X86/Latest.zip";
        private readonly string downloadUrl64 = "https://api.mct.mczlf.loft.games/007/Win_X64/Latest.zip";
        private readonly string dotnet8DownloadUrl64 = "https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/sdk-8.0.418-windows-x64-installer";
        private readonly string dotnet8DownloadUrl86 = "https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/sdk-8.0.418-windows-x86-installer";
        private string tempZipPath;
        private string tempExtractPath;
        private TaskCompletionSource<bool> userConfirmTcs;

        public update7()
        {
            InitializeComponent();
            InitializeDetailText();
        }

        private void InitializeDetailText()
        {
            detailText.Text = "MinecraftConnectTool 0.0.7 全新升级\n\n" +
                "本次升级是自底层开始的全面重写：\n\n" +
                "• 基于 .NET 8 框架开发，性能大幅提升\n" +
                "• 采用 MVVM 架构，代码更加清晰稳定\n" +
                "• 使用 Avalonia 跨平台框架\n" +
                "• 支持 Windows、Linux、macOS 多平台\n" +
                "• Android 版本基于 .NET MAUI 开发\n\n" +
                "系统要求：\n" +
                "• Windows 8 或更高版本（不再支持 Windows 7）\n" +
                "• .NET 8 运行时环境\n" +
                "• 64位或32位系统均可\n\n" +
                "点击【开始升级】按钮开始升级流程。";
        }

        private void steps1_ItemClick(object sender, AntdUI.StepsItemEventArgs e)
        {
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            if (userConfirmTcs != null && !userConfirmTcs.Task.IsCompleted)
            {
                userConfirmTcs.SetResult(true);
                return;
            }

            button4.Loading = true;
            button4.Enabled = false;

            try
            {
                await ExecuteStep1();
                await ExecuteStep2();
                await ExecuteStep3();
                await ExecuteStep4();
                await ExecuteStep5();
            }
            catch (Exception ex)
            {
                button4.Loading = false;
                button4.Enabled = true;
                button4.Text = "开始升级";
                ShowError("升级失败: " + ex.Message);
            }
        }

        private async Task ExecuteStep1()
        {
            UpdateStepStatus(0, AntdUI.TStepState.Process);
            detailText.Text = "请仔细阅读以下升级信息：\n\n" +
                "MinecraftConnectTool 0.0.7 来了!\n\n" +
                "主要改进：\n" +
                "• 底层重写，基于.NET 8 框架，性能提升显著\n" +
                "• MVVM 架构设计，全新UI设计，更稳定可靠\n" +
                "• Avalonia 跨平台 UI 框架\n" +
                "• 支持多平台部署\n\n" +
                "【重要提示】\n" +
                "• 升级后您可以手动回退到旧版本\n" +
                "• Windows 7 用户请勿升级,0.0.6仍会继续更新\n\n" +
                "点击【确认继续】开始升级流程。";

            userConfirmTcs = new TaskCompletionSource<bool>();
            button4.Loading = false;
            button4.Enabled = true;
            button4.Text = "确认继续";
            
            await userConfirmTcs.Task;
            
            button4.Loading = true;
            button4.Enabled = false;
            button4.Text = "开始升级";
            
            UpdateStepStatus(0, AntdUI.TStepState.Finish);
        }

        private async Task ExecuteStep2()
        {
            UpdateStepStatus(1, AntdUI.TStepState.Process);
            detailText.Text = "正在验证操作系统版本...";

            await Task.Delay(800);

            if (!IsWindows8OrHigher())
            {
                UpdateStepStatus(1, AntdUI.TStepState.Error);
                throw new Exception("您的操作系统版本过低。0.0.7版本要求Windows 8或更高版本，不支持Windows 7。");
            }

            string osVersion = GetWindowsVersion();
            detailText.Text = "操作系统验证通过！\n\n当前系统: " + osVersion + "\n\n符合升级要求，继续下一步...";

            await Task.Delay(1000);
            UpdateStepStatus(1, AntdUI.TStepState.Finish);
        }

        private async Task ExecuteStep3()
        {
            UpdateStepStatus(2, AntdUI.TStepState.Process);
            detailText.Text = "正在验证 .NET 8 运行环境...";

            await Task.Delay(800);

            if (!IsDotNet8Installed())
            {
                string dotnetUrl = Is64BitOperatingSystem() ? dotnet8DownloadUrl64 : dotnet8DownloadUrl86;
                
                detailText.Text = "未检测到 .NET 8 运行环境。\n\n" +
                    "MinecraftConnectTool 0.0.7 需要 .NET 8 运行时才能运行。\n\n" +
                    "点击【前往下载】按钮在浏览器中打开 .NET 8 SDK 下载页面，\n" +
                    "请下载并安装后返回本程序点击【安装完成】继续。";

                userConfirmTcs = new TaskCompletionSource<bool>();
                button4.Loading = false;
                button4.Enabled = true;
                button4.Text = "前往下载";
                
                await userConfirmTcs.Task;
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = dotnetUrl,
                    UseShellExecute = true
                });

                detailText.Text = "浏览器已打开下载页面。\n\n" +
                    "请完成 .NET 8 SDK 的安装，然后点击【安装完成】继续。";
                
                userConfirmTcs = new TaskCompletionSource<bool>();
                button4.Text = "安装完成";
                
                await userConfirmTcs.Task;
                
                button4.Loading = true;
                button4.Enabled = false;
                button4.Text = "开始升级";

                if (!IsDotNet8Installed())
                {
                    throw new Exception("未检测到 .NET 8 安装，请确保安装成功后再试。");
                }

                detailText.Text = ".NET 8 安装验证通过！\n\n继续下一步...";
                await Task.Delay(800);
            }
            else
            {
                detailText.Text = ".NET 8 运行环境已安装！\n\n继续下一步...";
                await Task.Delay(800);
            }

            UpdateStepStatus(2, AntdUI.TStepState.Finish);
        }

        private async Task ExecuteStep4()
        {
            UpdateStepStatus(3, AntdUI.TStepState.Process);
            detailText.Text = "正在准备下载新版本...";

            string downloadUrl = Is64BitOperatingSystem() ? downloadUrl64 : downloadUrl32;
            string arch = Is64BitOperatingSystem() ? "64位" : "32位";
            detailText.Text = "检测到您的系统是 " + arch + " 版本\n开始下载更新包...";

            progressBar.Visible = true;
            progressLabel.Visible = true;

            try
            {
                await DownloadFileAsync(downloadUrl);
                detailText.Text = "下载完成！\n\n正在准备解压...";
                UpdateStepStatus(3, AntdUI.TStepState.Finish);
            }
            catch (Exception ex)
            {
                UpdateStepStatus(3, AntdUI.TStepState.Error);
                throw new Exception("下载失败: " + ex.Message);
            }
            finally
            {
                progressBar.Visible = false;
                progressLabel.Visible = false;
            }
        }

        private async Task ExecuteStep5()
        {
            UpdateStepStatus(4, AntdUI.TStepState.Process);
            detailText.Text = "正在解压并准备启动新版本...";

            await Task.Delay(500);

            try
            {
                ExtractAndLaunch();
                UpdateStepStatus(4, AntdUI.TStepState.Finish);
                detailText.Text = "升级完成！\n\n新版本已启动，本程序即将关闭。";
                
                await Task.Delay(1500);
                Application.Exit();
            }
            catch (Exception ex)
            {
                UpdateStepStatus(4, AntdUI.TStepState.Error);
                throw new Exception("启动新版本失败: " + ex.Message);
            }
        }

        private void UpdateStepStatus(int stepIndex, AntdUI.TStepState state)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, AntdUI.TStepState>(UpdateStepStatus), stepIndex, state);
                return;
            }

            steps1.Current = stepIndex;
            steps1.Status = state;
            steps1.Invalidate();
        }

        private bool IsWindows8OrHigher()
        {
            Version osVersion = Environment.OSVersion.Version;
            return osVersion.Major > 6 || (osVersion.Major == 6 && osVersion.Minor >= 2);
        }

        private string GetWindowsVersion()
        {
            Version osVersion = Environment.OSVersion.Version;
            string versionName;

            if (osVersion.Major == 10 && osVersion.Build >= 22000)
                versionName = "Windows 11";
            else if (osVersion.Major == 10)
                versionName = "Windows 10";
            else if (osVersion.Major == 6 && osVersion.Minor == 3)
                versionName = "Windows 8.1";
            else if (osVersion.Major == 6 && osVersion.Minor == 2)
                versionName = "Windows 8";
            else if (osVersion.Major == 6 && osVersion.Minor == 1)
                versionName = "Windows 7";
            else
                versionName = "Windows " + osVersion.Major + "." + osVersion.Minor;

            return versionName + " (Build " + osVersion.Build + ")";
        }

        private bool Is64BitOperatingSystem()
        {
            return Environment.Is64BitOperatingSystem;
        }

        private bool IsDotNet8Installed()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = "--version";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    Version version;
                    if (Version.TryParse(output.Trim(), out version))
                    {
                        return version.Major >= 8;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private async Task DownloadFileAsync(string url)
        {
            tempZipPath = Path.Combine(Path.GetTempPath(), "MCT_Update_" + Guid.NewGuid().ToString() + ".zip");

            using (WebClient client = new WebClient())
            {
                var tcs = new TaskCompletionSource<bool>();

                client.DownloadProgressChanged += (s, e) =>
                {
                    progressBar.Value = e.ProgressPercentage;
                    progressLabel.Text = "下载进度: " + e.ProgressPercentage + "% (" + FormatBytes(e.BytesReceived) + " / " + FormatBytes(e.TotalBytesToReceive) + ")";
                };

                client.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null)
                        tcs.SetException(e.Error);
                    else if (e.Cancelled)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(true);
                };

                client.DownloadFileAsync(new Uri(url), tempZipPath);
                await tcs.Task;
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return size.ToString("0.##") + " " + sizes[order];
        }

        private void ExtractAndLaunch()
        {
            tempExtractPath = Path.Combine(Path.GetTempPath(), "MCT_Update_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempExtractPath);

            ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

            string exePath = Path.Combine(tempExtractPath, "Latest.exe");
            if (!File.Exists(exePath))
            {
                string[] files = Directory.GetFiles(tempExtractPath, "Latest.exe", SearchOption.AllDirectories);
                if (files.Length > 0)
                    exePath = files[0];
                else
                    throw new FileNotFoundException("在下载的包中找不到 Latest.exe");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true
            });

            Task.Run(() =>
            {
                try
                {
                    if (File.Exists(tempZipPath))
                        File.Delete(tempZipPath);
                }
                catch { }
            });
        }

        private void ShowError(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ShowError), message);
                return;
            }

            detailText.Text = "错误: " + message + "\n\n请检查网络连接或联系技术支持。";
            MessageBox.Show(message, "升级错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("https://mct.mczlf.loft.games/function/download");
        }
    }
}
