using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace locationserver
{

    class Program
    {
        [DllImport("Kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();
        public static TcpListener listener;
        public static SQLiteConnection sqlConnection;
        public static SQLiteCommand cmd = new SQLiteCommand(sqlConnection);
        public static string readline;

        [STAThread]

        public static int Main(string[] args)
        {

            FreeConsole();
            var app = new App();
            return app.Run();

        }

        public static void runServer()
        {
            if (!File.Exists("database.db"))
            {
                SQLiteConnection.CreateFile("database.db");
            }
            sqlConnection = new SQLiteConnection("Data Source=database.db;Version=3;New=True;Compress=True;");

            sqlConnection.Open();
            CreateTable(sqlConnection);




            //TcpListener listener;
            Socket connection;
            Handler requesthandler;
            try
            {
                listener = new TcpListener(IPAddress.Any, 43);
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
                StreamWriter sw = new StreamWriter(socketStream);
                StreamReader sr = new StreamReader(socketStream);
                sw.AutoFlush = true;  // sw flushes automatically
                                      //socketStream.ReadTimeout = 1000;
                                      //socketStream.WriteTimeout = 1000;

                try
                {

                    readline = sr.ReadLine();
                    string[] fileName = readline.Split(',');
                    var itemList = fileName.ToList();
                    itemList.RemoveRange(0, 2);
                    itemList.RemoveAll(string.IsNullOrEmpty);
                    switch (fileName[0])
                    {
                        case "paid":
                            InsertData(fileName);
                            break;
                        case "create":
                            StreamWriter writer = new StreamWriter(fileName[1], true);
                            writer.Close();
                            break;
                        case "write":

                            File.AppendAllLines(fileName[1], itemList);
                            break;
                        case "read":
                            string[] readFile = File.ReadAllLines(fileName[1]);
                            string sendItems = string.Join(",", readFile.ToArray());
                            sw.WriteLine(sendItems);
                            sw.Close();
                            break;

                        default:
                            break;
                    }


                }

                catch (Exception ex)
                {
                    Console.WriteLine("error: " + ex);

                }
                finally
                {
                    socketStream.Close();
                    connection.Close();
                    //sqlConnection.Close();                   
                }


            }
            public void InsertData(string[] allText)
            {

                try
                {
                    SQLiteCommand sQLiteCommand;
                    sQLiteCommand = sqlConnection.CreateCommand();
                    sQLiteCommand.CommandText = "INSERT INTO PaidTable(OrderId, DataTime, TableNumber, Amount, DiscountAmount, PaymentMethod) values (@param1, @param2,@param3,@param4,@param5,@param6)";
                    sQLiteCommand.Parameters.AddWithValue("@param1", allText[2]);
                    sQLiteCommand.Parameters.AddWithValue("@param2", allText[3]);
                    sQLiteCommand.Parameters.AddWithValue("@param3", allText[4]);
                    sQLiteCommand.Parameters.AddWithValue("@param4", allText[5]);
                    sQLiteCommand.Parameters.AddWithValue("@param5", allText[6]);
                    sQLiteCommand.Parameters.AddWithValue("@param6", allText[7]);
                    sQLiteCommand.Prepare();
                    sQLiteCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {

                }
            }


        }

        public static void CreateTable(SQLiteConnection connection)
        {
            SQLiteCommand sQCommand;
            string orderTable = "CREATE TABLE IF NOT EXISTS Orders (OrderId INT, DateTime DATETIME, TableNumber INT, Course TEXT, DishName TEXT, Price DOUBLE, DiscountPrice DOUBLE, DiscountBool INT, PaidBool INT, PaymentMethod TEXT)";
            string paidTable = "CREATE TABLE IF NOT EXISTS PaidTable (OrderId INT, DataTime DATATIME, TableNumber INT, Amount DOUBLE, DiscountAmount DOUBLE, PaymentMethod TEXT)";

            sQCommand = connection.CreateCommand();
            sQCommand.CommandText = orderTable;
            sQCommand.ExecuteNonQuery();

            sQCommand.CommandText = paidTable;
            sQCommand.ExecuteNonQuery();

        }

    }

}

