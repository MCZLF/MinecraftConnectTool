using System;
using System.Net;
using System.IO;

namespace MinecraftConnectTool
{
    internal class supportcheck
    {
        public static void Check()
        {
            try
            {
                var list = new WebClient().DownloadString("https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/006/SupportVer").Split('\n');
                foreach (var v in list)
                {
                    if (v.Trim() == Form1.version)
                    {
                        SupportVer();
                        return;
                    }
                }
                NotSupport();
            }
            catch {  }
        }

        static async void NotSupport()
        {
            try
            {
                var cloud = (await new WebClient().DownloadStringTaskAsync("https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/version006.txt")).Trim();
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "APPLog.ini"), "NotSupportNow");
                Program.alertwarn($"当前版本不在支持列表内,请检查更新是否可用\n若发现有Bug,请勿反馈\n当前版本{Form1.version}   云版本{cloud}");
            }
            catch { }
        }
        static void SupportVer() { try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp", "APPLog.ini"), "SupportVersion"); } catch { } }
    }
}