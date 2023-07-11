// 服务器端代码
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class Program
    {
        // 定义一个服务器 Socket，用来监听客户端的连接请求
        static Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // 定义一个字典，用来存储客户端的虚拟 IP 地址和 Socket 对象的映射关系
        static Dictionary<string, Socket> clients = new Dictionary<string, Socket>();

        // 定义一个随机数生成器，用来生成虚拟 IP 地址
        static Random random = new Random();

        static void Main(string[] args)
        {
            // 绑定服务器的 IP 地址和端口号
            server.Bind(new System.Net.IPEndPoint(IPAddress.Any, 23333));

            // 开始监听客户端的连接请求
            server.Listen(256);

            Console.WriteLine("服务器已启动，等待客户端的连接...");

            // 开启一个新的线程，用来接受客户端的连接请求
            Thread acceptThread = new Thread(AcceptClient);
            acceptThread.IsBackground = true;
            acceptThread.Start();

            Console.ReadLine();
        }

        // 接受客户端的连接请求的方法
        static void AcceptClient()
        {
            while (true)
            {
                try
                {
                    // 接受一个客户端的连接请求，并返回一个与该客户端通信的 Socket 对象
                    Socket client = server.Accept();
                    Console.WriteLine("接受一个客户端的连接请求，IP 地址为：" + client.RemoteEndPoint.ToString());
                    // 生成一个虚拟 IP 地址，作为该客户端的标识（这里简单地使用随机数生成，实际情况可能需要考虑重复或冲突等问题）
                    string vip ="192.168.2."+ random.Next(1, 255);
                    Console.WriteLine("分配给该客户端的虚拟 IP 地址为：" + vip);
                    // 将虚拟 IP 地址和 Socket 对象添加到字典中
                    clients.Add(vip, client);
                    // 将虚拟 IP 地址发送给客户端
                    client.Send(System.Text.Encoding.UTF8.GetBytes(vip));

                    Console.WriteLine("发送虚拟 IP 地址给客户端");

                    // 开启一个新的线程，用来接收该客户端发送的数据包
                    Thread receiveThread = new Thread(ReceiveData);
                    receiveThread.IsBackground = true;
                    receiveThread.Start(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("接受客户端的连接请求时发生异常：" + ex.Message);
                }
            }
        }

        // 接收客户端发送的数据包的方法
        static void ReceiveData(object obj)
        {
            // 将参数转换为 Socket 对象
            Socket client = obj as Socket;

            // 定义一个缓冲区，用来存储接收到的数据包
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    // 接收客户端发送的数据包，并返回实际接收到的字节数
                    int length = client.Receive(buffer);

                    // 如果接收到的字节数为 0，表示客户端已断开连接，退出循环
                    if (length == 0)
                    {
                        break;
                    }

                    Console.WriteLine("接收到客户端发送的数据包，长度为：" + length + "内容为：" + buffer);

                    // 获取数据包前面的虚拟 IP 地址，作为目标客户端的标识
                    string vip = System.Text.Encoding.UTF8.GetString(buffer, 0, 15);

                    Console.WriteLine("目标客户端的虚拟 IP 地址为：" + vip);

                    // 从字典中根据虚拟 IP 地址找到对应的 Socket 对象
                    Socket target = clients[vip];

                    // 将数据包转发给目标客户端
                    target.Send(buffer, 15, length - 15, SocketFlags.None);

                    Console.WriteLine("转发数据包给目标客户端");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("接收客户端发送的数据包时发生异常：" + ex.Message);
                    break;
                }
            }

            // 客户端断开连接后，从字典中移除该客户端的虚拟 IP 地址和 Socket 对象，并关闭 Socket
            foreach (var item in clients)
            {
                if (item.Value == client)
                {
                    clients.Remove(item.Key);
                    break;
                }
            }
            client.Close();
            Console.WriteLine("已断开与客户端的连接");
        }
    }
}