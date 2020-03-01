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
        public static MainWindow mainWindow;


        [STAThread]

        public static int Main(string[] args)
        {

            FreeConsole();
            var app = new App();

            return app.Run();

        }
        public static void getWindow(MainWindow window)
        {
            mainWindow = window;
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
                StreamWriter sw = new StreamWriter(socketStream);
                StreamReader sr = new StreamReader(socketStream);
                sw.AutoFlush = true;  // sw flushes automatically
                                      //socketStream.ReadTimeout = 1000;
                                      //socketStream.WriteTimeout = 1000;



                try
                {

                    readline = sr.ReadLine();
                    //mainWindow.Dispatcher.Invoke(() =>
                    //{

                    //    mainWindow.view_button.Text += "\r\nConnection Received";
                    //});
                    
                        string[] fileName = readline.Split(',');
                        var itemList = fileName.ToList();
                        itemList.RemoveRange(0, 2);
                    try
                    {
                        itemList.RemoveAll(string.IsNullOrEmpty);
                    }
                    catch
                    {

                    }

                    switch (fileName[0])
                    {
                        case "itemsList":
                            string sendBack= readItems();
                            sw.WriteLine(sendBack);
                            sw.Close();
                            break;
                        case "reset":
                            resetXread();
                            break;
                        case "paid":
                            InsertData(fileName);
                            File.Delete(fileName[1]);
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
                            //mainWindow.view_button.Text += "\r\nConnection Sent";
                            sw.WriteLine(sendItems);
                            sw.Close();
                            break;
                        case "requestXRead":
                            string readBack = requestXRead();
                            sw.WriteLine(readBack);
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

            private string readItems()
            {
                string read = "";
                try
                {
                    SQLiteDataReader sqlite_dataReader;
                    SQLiteCommand sqlite_command;
                    sqlite_command = sqlConnection.CreateCommand();
                    sqlite_command.CommandText = "SELECT Item, Price FROM ItemList";
                    sqlite_dataReader = sqlite_command.ExecuteReader();

                    while (sqlite_dataReader.Read())
                    {
                        
                        read += sqlite_dataReader.GetString(0)+",";
                        read += sqlite_dataReader.GetDouble(1)+",";
                       

                    }
                }
                catch (Exception e)
                {

                }
                return read;
            }

            public void InsertData(string[] allText)
            {

                try
                {
                    SQLiteCommand sQLiteCommand;
                    sQLiteCommand = sqlConnection.CreateCommand();
                    sQLiteCommand.CommandText = "INSERT INTO PaidTable(OrderId, DataTime, TableNumber, Amount, DiscountAmount, PaymentMethod, Reset) values (@orderid, @datetime,@tablenumber,@amount,@discount,@paymentmethod,@reset)";
                    sQLiteCommand.Parameters.AddWithValue("@orderid", allText[2]);
                    sQLiteCommand.Parameters.AddWithValue("@datetime", allText[3]);
                    sQLiteCommand.Parameters.AddWithValue("@tablenumber", allText[4]);
                    sQLiteCommand.Parameters.AddWithValue("@amount", allText[5]);
                    sQLiteCommand.Parameters.AddWithValue("@discount", allText[6]);
                    sQLiteCommand.Parameters.AddWithValue("@paymentmethod", allText[7]);
                    sQLiteCommand.Parameters.AddWithValue("@reset", allText[8]);
                    sQLiteCommand.Prepare();
                    sQLiteCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {

                }
            }


        }
        public static void resetXread()
        {
            SQLiteCommand command;
            command = sqlConnection.CreateCommand();
            command.CommandText = "update PaidTable set Reset =1 where Reset=0;";
            command.ExecuteNonQuery();

        }

        public static string requestXRead()
        {
            string readData = "";
            try
            {
                SQLiteDataReader sqlite_dataReader;
                SQLiteCommand sqlite_command;
                sqlite_command = sqlConnection.CreateCommand();
                sqlite_command.CommandText = "SELECT * FROM PaidTable WHERE Reset='0'";
                sqlite_dataReader = sqlite_command.ExecuteReader();

                while (sqlite_dataReader.Read())
                {
                    readData += ",amount,";
                    readData += sqlite_dataReader.GetDouble(3);
                    readData += ",discount,";
                    readData += sqlite_dataReader.GetDouble(4);
                    readData += ",paymentMethod,";
                    readData += sqlite_dataReader.GetString(5);
                 
                }
            }
            catch (Exception e)
            {

            }

            return readData;

        }

        public static void CreateTable(SQLiteConnection connection)
        {
            SQLiteCommand sQCommand;
            string orderTable = "CREATE TABLE IF NOT EXISTS Orders (OrderId INT, DateTime DATETIME, TableNumber INT, Course TEXT, DishName TEXT, Price DOUBLE, DiscountPrice DOUBLE, DiscountBool INT, PaidBool INT, PaymentMethod TEXT)";
            string paidTable = "CREATE TABLE IF NOT EXISTS PaidTable (OrderId INT, DataTime DATATIME, TableNumber INT, Amount DOUBLE, DiscountAmount DOUBLE, PaymentMethod TEXT, Reset INT)";

            sQCommand = connection.CreateCommand();
            sQCommand.CommandText = orderTable;
            sQCommand.ExecuteNonQuery();

            sQCommand.CommandText = paidTable;
            sQCommand.ExecuteNonQuery();

        }

    }

}

