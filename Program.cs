using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCPserver
{
    class Program
    {
        static int port;
        static IPAddress serverip;
        static string[] znach = new string[] {"Привет, " , "Как дела, ","Что делаешь, ","Рад тебя видеть, "};
        static Random rnd = new Random();
        static ServerObject server;
        static Thread listenThread;
        public static List<ClientObject> clients = new List<ClientObject>();
        static void Main(string[] args)
        {
            Console.WriteLine("Введите ip для сервера");
            serverip = IPAddress.Parse(Console.ReadLine());
            Console.WriteLine("Введите порт для сервера");
            port = int.Parse(Console.ReadLine());
            Console.Clear();
            try
            {
                server = new ServerObject();
                listenThread = new Thread(new ThreadStart(server.Listen));
                listenThread.Start(); 
            }
            catch (Exception ex)
            {
                server.Disconnect();
                Console.WriteLine(ex.Message);
            }

        }
        public class ClientObject
        {
            protected internal string Id { get; private set; }
            protected internal NetworkStream Stream { get; private set; }
            TcpClient client;
            ServerObject server;

            public ClientObject(TcpClient tcpClient, ServerObject serverObject)
            {
                Id = Guid.NewGuid().ToString();
                client = tcpClient;
                server = serverObject;
                serverObject.AddConnection(this);
            }
            protected internal int GetClientNumber(string id)
            {
                ClientObject client = clients.FirstOrDefault(c => c.Id == id);
                if (client != null)
                    return clients.IndexOf(client);
                return 0;
            }

            public void Process()
            {
                try
                {
                    Stream = client.GetStream();
                    
                    while (true)
                    {
                        try
                        {
                            if (GetClientNumber(Id) <= 4)
                            {                                                               
                                Thread.Sleep(6000);
                                string message = GetMessage();
                                Console.WriteLine($"Поток номер {GetClientNumber(Id)}, " + DateTime.Now.ToShortTimeString() + ": " + message.ToString());
                                string builder = znach[rnd.Next(0, 4)] + message.ToString();
                                SendMessage(builder);
                            }
                            else
                            {
                                string message = GetMessage();
                                string builder = "Превышен лимит потоков, попробуйте позже :с";
                                SendMessage(builder);
                            }                          
                        }
                        catch
                        {
                            string message = String.Format($"Поток номер {GetClientNumber(Id)} офнул");
                            Console.WriteLine(message);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    server.RemoveConnection(this.Id);                   
                    Close();
                }
            }
            private string GetMessage()
            {
                byte[] data = new byte[256]; 
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = Stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (Stream.DataAvailable);

                return builder.ToString();
            }
            private void SendMessage(string builder)
            {
                byte[] data = new byte[256];
                data = Encoding.Unicode.GetBytes(builder);
                Stream.Write(data, 0, data.Length);
            }

            protected internal void Close()
            {
                if (Stream != null)
                    Stream.Close();
                if (client != null)
                    client.Close();
            }
        }

        public class ServerObject
        {
            static TcpListener tcpListener;

            protected internal void AddConnection(ClientObject clientObject)
            {
                clients.Add(clientObject);
            }
            protected internal void RemoveConnection(string id)
            {
                ClientObject client = clients.FirstOrDefault(c => c.Id == id);
                if (client != null)
                    clients.Remove(client);               
            }
            protected internal void Listen()
            {
                try
                {
                    tcpListener = new TcpListener(serverip, port);
                    tcpListener.Start();
                    Console.WriteLine("Сервер запущен. Седим пердим...");

                    while (true)
                    {

                        TcpClient tcpClient = tcpListener.AcceptTcpClient();
                        ClientObject clientObject = new ClientObject(tcpClient, this);
                        Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                        clientThread.Start();

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Disconnect();
                }
            }

            protected internal void Disconnect()
            {
                tcpListener.Stop(); 

                for (int i = 0; i < clients.Count; i++)
                {                   
                    clients[i].Close(); 
                }
                Environment.Exit(0); 
            }
        }
    }
}
