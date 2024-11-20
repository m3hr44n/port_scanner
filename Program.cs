using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System;

namespace PortScanner
{
    class Program
    {
        static bool stop = false;
        static int startPort;
        static int endPort;

        static List<int> openPorts = new List<int>();

        static object consoleLock = new object();

        static int waitingForResponses;

        static int maxQueriesAtOneTime = 100;

        static void Main(string[] args)
        {
        begin:
            Console.WriteLine("Enter the IP Address of the target:");
            string ip = Console.ReadLine();

            IPAddress ipAddress;

            if (!IPAddress.TryParse(ip, out ipAddress))
                goto begin;

            startP:

            Console.WriteLine("Enter the starting port to start scanning on:");
            string sp = Console.ReadLine();

            if (!int.TryParse(sp, out startPort))
                goto startP;

            endP:

            Console.WriteLine("Enter the end port:");
            string ep = Console.ReadLine();

            if (!int.TryParse(ep, out endPort))
                goto endP;

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("Press any key to stop scanning...");

            Console.WriteLine("");
            Console.WriteLine("");

            ThreadPool.QueueUserWorkItem(StartScan, ipAddress);

            Console.ReadKey();

            stop = true;

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void StartScan(object o)
        {
            IPAddress ipAddress = o as IPAddress;

            for (int i = startPort; i < endPort; i++)
            {
                lock (consoleLock)
                {
                    int top = Console.CursorTop;

                    Console.CursorTop = 7;
                    Console.WriteLine("Scanning port: {0}    ", i);

                    Console.CursorTop = top;
                }

                while (waitingForResponses >= maxQueriesAtOneTime)
                    Thread.Sleep(0);

                if (stop)
                    break;

                try
                {
                    Socket s = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Stream, ProtocolType.Tcp);

                    s.BeginConnect(new IPEndPoint(ipAddress, i), EndConnect, s);

                    Interlocked.Increment(ref waitingForResponses);
                }
                catch (Exception)
                {

                }
            }
        }

        static void EndConnect(IAsyncResult ar)
        {
            try
            {
                DecrementResponses();

                Socket s = ar.AsyncState as Socket;

                s.EndConnect(ar);

                if (s.Connected)
                {
                    int openPort = Convert.ToInt32(s.RemoteEndPoint.ToString().Split(':')[1]);

                    openPorts.Add(openPort);

                    lock (consoleLock)
                    {
                        Console.WriteLine("Connected TCP on port: {0}", openPort);
                    }

                    s.Disconnect(true);
                }
            }
            catch (Exception)
            {

            }
        }

        static void IncrementResponses()
        {
            Interlocked.Increment(ref waitingForResponses);

            PrintWaitingForResponses();
        }

        static void DecrementResponses()
        {
            Interlocked.Decrement(ref waitingForResponses);

            PrintWaitingForResponses();
        }

        static void PrintWaitingForResponses()
        {
            lock (consoleLock)
            {
                int top = Console.CursorTop;

                Console.CursorTop = 8;
                Console.WriteLine("Waiting for responses from {0} sockets ", waitingForResponses);

                Console.CursorTop = top;
            }
        }
    }
}