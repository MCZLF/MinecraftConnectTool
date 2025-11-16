using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

namespace MinecraftConnectTool
{
    internal class Server_Post
    {
        public static Thread Post_Thread = null;
        private static bool isRunning = false; // 控制多播线程运行状态的标志

        // 启动多播发送
        public static void Post_Main(int post)
        {
            string multicastGroup = "224.0.2.60";
            int multicastPort = 4445;

            using (UdpClient client = new UdpClient(post))
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(multicastGroup), multicastPort);

                byte[] ttl = new byte[] { 2 }; // 多播数据包的存活时间
                client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl);

                isRunning = true; // 设置标志为运行状态
                while (isRunning)
                {
                    string message = $"[MOTD]§b§l[MCT][局域网多播插件] §2局域网世界 §bServerPost[/MOTD][AD]{post}[/AD]";
                    byte[] data = Encoding.UTF8.GetBytes(message);

                    client.Send(data, data.Length, remoteEP);

                    Thread.Sleep(100);
                }
            }
        }

        // 停止多播发送
        public static void Stop_Post()
        {
            try
            {
                isRunning = false; // 设置标志为停止状态
                if (Post_Thread != null)
                {
                    Post_Thread.Join(); // 等待线程结束
                    Post_Thread = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("关闭多播线程时发生异常: " + ex.Message);
            }
        }

        // 启动多播发送线程
        public static void Start_Post(int port)
        {
            if (Post_Thread == null || !Post_Thread.IsAlive)
            {
                Post_Thread = new Thread(() => Post_Main(port));
                Post_Thread.Start();
            }
            else
            {
                Console.WriteLine("多播线程已经在运行！");
            }
        }

        // 其他方法保持不变
        public static void Server_Get(int post, int mbpost)
        {
            int port = post; 

            TcpListener server = new TcpListener(IPAddress.Loopback, port);
            server.Start();
            Console.WriteLine("Server started. Listening for connections...");

            try
            {
                while (true)  // 无限循环等待客户端连接
                {
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // 处理客户端连接的独立线程或任务
                    // 使用异步方式可以提高性能，这里使用同步方式演示
                    HandleClient(client, mbpost);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server exception: " + ex);
            }
        }

        private static void HandleClient(TcpClient client, int mbdk)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream);
                StreamWriter writer = new StreamWriter(stream);

                while (true)
                {
                    string request = reader.ReadLine();
                    if (request == null) break;

                    Console.WriteLine("Received: " + request);
                    string response = "Echo: " + request; // 示例响应
                    if (request == "post")
                    {
                        response = mbdk.ToString();
                    }
                    Console.WriteLine($"{response}\n{response}");
                    writer.WriteLine(response);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while handling client: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        public static string get_web(string serverIp, int port, string text)
        {
            try
            {
                // 创建TcpClient对象，连接到服务器
                TcpClient client = new TcpClient(serverIp, port);
                Console.WriteLine("Connected to server.");

                // 获取流对象，用于读写数据
                NetworkStream stream = client.GetStream();

                // 发送信息到服务器
                string messageToSend = text;
                byte[] data = Encoding.ASCII.GetBytes(messageToSend);
                stream.Write(data, 0, data.Length);
                Console.WriteLine("Message sent to server.");

                // 接收服务器返回的信息
                byte[] buffer = new byte[1024]; // 定义接收缓冲区大小
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Response received: " + response);

                // 关闭TcpClient连接
                client.Close();

                return response;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: " + ex);
                return "dontget";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
                return "dontget";
            }
        }
    }
}