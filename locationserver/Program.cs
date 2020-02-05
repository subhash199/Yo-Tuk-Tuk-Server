﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqLite.Core;

namespace locationserver
{

    class Program
    {
        [DllImport("Kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();
        public static TcpListener listener;



        [STAThread]

        public static int Main(string[] args)
        {
            if (!args.Contains("-w"))
            {
                runServer();

                return 0;
            }
            else
            {
                FreeConsole();
                var app = new App();
                return app.Run();
            }




        }
    
        public static void runServer()
        {
            //TcpListener listener;
            Socket connection;
            Handler requesthandler;
            try
            {
                listener = new TcpListener(IPAddress.Any, 5002);
                listener.Start();
                Console.WriteLine("Server is Listening");
                while (true)
                {
                    connection = listener.AcceptSocket();
                    requesthandler = new Handler();
                    Thread t = new Thread(() => requesthandler.clientRequest(connection));
                    t.Start();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Execption: " + e.ToString());
            }
        }

        class Handler
        {
            public Handler()
            {

            }
            public void clientRequest(Socket connection)
            {

                NetworkStream socketStream = new NetworkStream(connection);
                Console.WriteLine("Connection Received");
                try
                {
                    StreamWriter sw = new StreamWriter(socketStream);
                    StreamReader sr = new StreamReader(socketStream);

                    sw.AutoFlush = true;// sw flushes automatically
                                        //socketStream.ReadTimeout = 1000;
                                        //socketStream.WriteTimeout = 1000;


                }

                catch (Exception ex)
                {
                    Console.WriteLine("error: " + ex);

                }
                finally
                {
                    socketStream.Close();
                    connection.Close();
                }


            }
        }



    }

}

