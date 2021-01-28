using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace server
{
    static class Server
    {
        static void Main(String[] args)
        {
            Console.WriteLine("\nWelche IP Adresse soll's denn sein?\n");

            int i = 0, pick;

            List<IPAddress> ips = GetV4Ips();

            if (ips.Count == 0)
            {
                Console.WriteLine("Keine IP Adressen gefunden.");
                Console.ReadLine();
                Environment.Exit(0);
            }
            else
            {
                i = 1;
                Console.WriteLine();
                foreach (IPAddress ip in ips)
                {
                    Console.WriteLine(ip.ToString() + ": " + i.ToString());
                    i++;
                }
                Console.WriteLine();
            }

            do
            {
                try
                {
                    pick = Convert.ToInt32(Console.ReadLine());
                }
                catch
                {
                    pick = 0;
                }
            }
            while (pick < 1 || pick > i - 1);

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(new IPEndPoint(ips.ElementAt(pick - 1), 10000));
            serverSocket.Listen((int)SocketOptionName.MaxConnections);

            Console.WriteLine("\nZeitserver unter {0}:10000 gestartet ...", ips.ElementAt(pick - 1));

            while (true)
            {
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // blocks until a client connects
                clientSocket = serverSocket.Accept();

                //Hashtable clientList = new Hashtable();            

                //clientList.Add(nickname, clientSocket);

                Thread thread1 = new Thread(() => ServerThread(clientSocket));
                //Thread thread2 = new Thread(() => ServerThread(clientSocket));
                thread1.Start();
                //thread2.Start();
            }
        }

        private static List<IPAddress> GetV4Ips()
        {
            List<IPAddress> allIps = new List<IPAddress>(Dns.GetHostAddresses(Dns.GetHostName())),
                            v4Ips = new List<IPAddress>();

            foreach (IPAddress ip in allIps)
            {
                if (ip.AddressFamily.Equals(AddressFamily.InterNetwork))
                {
                    v4Ips.Add(ip);
                }
            }
            return v4Ips;
        }

        private static void ServerThread(Socket clientSocket)
        {
            Hashtable clientList = new Hashtable();
            
            Object thisLock = new Object();

            String dataFromClient = String.Empty;
            String dataToClient = String.Empty;

            Byte[] bytesTo = new Byte[256];
            Byte[] bytesFrom = new Byte[256];

            string nickname, msg;

            while (true)
            {
                try
                {
                    clientSocket.Receive(bytesFrom);

                    dataFromClient = Encoding.ASCII.GetString(bytesFrom).TrimEnd('\0');

                    Console.WriteLine("\nClient sagt: " + dataFromClient);

                    nickname = dataFromClient.Substring(0, dataFromClient.Length - 1);
                    
                    if (dataFromClient.Contains("+"))
                    {                       
                        lock (thisLock)
                        {
                            if (!clientList.Contains(nickname))
                            {
                                dataToClient = nickname + " hat sich angemeldet";

                                Console.WriteLine("\nServer antwortet: " + dataToClient);

                                bytesTo = Encoding.ASCII.GetBytes(dataToClient);

                                clientList.Add(nickname, clientSocket);

                                foreach (DictionaryEntry dE in clientList)
                                {
                                    Socket s = (Socket)dE.Value;
                                    s.Send(bytesTo);
                                }
                            }
                            else 
                            {
                                dataToClient = "~ Sorry, der Name " + nickname + " ist schon vergeben ~";

                                bytesTo = Encoding.ASCII.GetBytes(dataToClient);
                            }
                        }
                    }
                    else if (dataFromClient.Contains("-"))
                    {
                        if (clientList.Contains(nickname))
                        {
                            dataToClient = nickname + " hat sich abgemeldet";

                            Console.WriteLine("\nServer antwortet: " + dataToClient);

                            bytesTo = Encoding.ASCII.GetBytes(dataToClient);

                            clientList.Remove(nickname);

                            foreach (DictionaryEntry dE in clientList)
                            {
                                Socket s = (Socket)dE.Value;
                                s.Send(bytesTo);
                            }
                        }
                    }
                    else if (dataFromClient.Contains("*"))
                    {
                        dataToClient = nickname + " hat sich abgemeldet";

                        Console.WriteLine("\nServer antwortet: " + dataToClient);

                        bytesTo = Encoding.ASCII.GetBytes(dataToClient);

                        clientList.Remove(nickname);

                        clientSocket.Close();

                        foreach (DictionaryEntry dE in clientList)
                        {
                            Socket s = (Socket)dE.Value;
                            s.Send(bytesTo);
                        }
                    }
                    else if (dataFromClient.Contains("#"))
                    {
                        msg = dataFromClient.Substring(0, dataFromClient.Length - 1);

                        dataToClient = msg;

                        bytesTo = Encoding.ASCII.GetBytes(dataToClient);

                        foreach (DictionaryEntry dE in clientList)
                        {
                            Socket s = (Socket)dE.Value;
                            s.Send(bytesTo);
                        }
                    }
                }
                catch
                {                   
                  return;
                }
            }
        }
    }
}