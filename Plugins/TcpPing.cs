using System;
using System.Net.Sockets;
using System.Threading;

namespace MinecraftConnectTool
{
    public class TcpPing
    {
        public event Action<string, int> OnPingResult; // 事件，用于通知UI延迟结果

        private Thread pingThread;
        private bool isPinging;
        private string ipAddress;
        private int port;
        private int interval;

        public TcpPing(string ipAddress, int port, int interval = 1000)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.interval = interval;
        }

        public void Start()
        {
            isPinging = true;
            pingThread = new Thread(StartPing);
            pingThread.IsBackground = true;
            pingThread.Start();
        }

        public void Stop()
        {
            isPinging = false;
            if (pingThread != null && pingThread.IsAlive)
            {
                // 在后台线程里等它结束
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { pingThread.Join(); }
                    catch { /* ignore */ }
                });
            }
        }

        private void StartPing()
        {
            while (isPinging)
            {
                try
                {
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket.ReceiveTimeout = 3000;
                        socket.SendTimeout = 3000;

                        DateTime startTime = DateTime.Now;
                        socket.Connect(ipAddress, port);
                        DateTime endTime = DateTime.Now;

                        int latency = (int)(endTime - startTime).TotalMilliseconds;

                        OnPingResult?.Invoke(ipAddress, latency); // 通知UI
                    }
                }
                catch (SocketException)
                {
                    OnPingResult?.Invoke(ipAddress, -1); // 通知UI失败
                }
                catch (ThreadInterruptedException)
                {
                    // 线程被中断，退出循环
                    break;
                }

                Thread.Sleep(interval);
            }
        }
    }
}