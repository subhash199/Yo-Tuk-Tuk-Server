using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Data.SQLite;
using System.Threading;
using System.Runtime.InteropServices;
using System.Drawing.Printing;

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
        public static List<string> printList = new List<string>();
        public static string printerName = "Yo-Tuk-Tuk";
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



                NetworkStream socketStream = new NetworkStream(connection); // exception
                Console.WriteLine("Connection Received");
                StreamWriter sw = new StreamWriter(socketStream);
                StreamReader sr = new StreamReader(socketStream);
                sw.AutoFlush = true;  // sw flushes automatically
                socketStream.ReadTimeout = 1000;
                socketStream.WriteTimeout = 1000;

                try
                {

                    readline = sr.ReadLine();


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

                        case "logIn":
                            string respond = UserID(fileName);
                            sw.WriteLine(respond);
                            break;
                        case "signUp":
                            string response = UserID(fileName);
                            sw.WriteLine(response);
                            break;

                        case "listAll":
                            string listItems = readItems("listAll");
                            sw.WriteLine(listItems);
                            break;
                        case "itemsList":
                            string sendBack = readItems("itemsList");
                            sw.WriteLine(sendBack);
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
                            sw.WriteLine("OK");
                            break;
                        case "read":
                            string[] readFile = File.ReadAllLines(fileName[1]);
                            string sendItems = string.Join(",", readFile.ToArray());
                            //mainWindow.view_button.Text += "\r\nConnection Sent";
                            sw.WriteLine(sendItems);
                            break;
                        case "requestXRead":
                            string readBack = requestXRead();
                            sw.WriteLine(readBack);
                            break;
                        case "updateDetails":
                            InsertData(fileName);
                            break;
                        case "holdPrint":
                            printList = new List<string>(fileName);
                            PrintReceipt();
                            break;
                        case "printReceipt":
                            printList = new List<string>(fileName);
                            PrintReceipt();
                            break;

                        default:
                            break;
                    }
                    sw.Close();

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

            private string UserID(string[] fileName)
            {
                bool isExist = false;
                string returnString = "";
                SQLiteCommand sQLiteCommand;
                SQLiteDataReader sqlite_dataReader;
                sQLiteCommand = sqlConnection.CreateCommand();
                if (fileName[0] == "signUp")
                {

                    sQLiteCommand.CommandText = "SELECT * FROM Users WHERE LogInId =@id";
                    sQLiteCommand.Parameters.AddWithValue("@id", fileName[2]);
                    sqlite_dataReader = sQLiteCommand.ExecuteReader();

                    while (sqlite_dataReader.Read())
                    {
                        if (fileName[2] == sqlite_dataReader.GetInt16(2).ToString())
                        {
                            isExist = true;
                        }

                    }
                    sqlite_dataReader.Close();
                    if (isExist == false)
                    {
                        sQLiteCommand.CommandText = "INSERT INTO Users (Name, LogInId) values (@name,@login)";
                        sQLiteCommand.Parameters.AddWithValue("@name", fileName[1]);
                        sQLiteCommand.Parameters.AddWithValue("@login", fileName[2]);
                        sQLiteCommand.Prepare();
                        sQLiteCommand.ExecuteNonQuery();
                        returnString = "OK";
                    }
                    else
                    {
                        returnString = "userExists";
                    }


                }
                else
                {
                    sQLiteCommand.CommandText = "Select * FROM Users WHERE LogInId =@id";
                    sQLiteCommand.Parameters.AddWithValue("@id", fileName[1]);
                    sqlite_dataReader = sQLiteCommand.ExecuteReader();
                    while (sqlite_dataReader.Read())
                    {

                        if (fileName[1] == sqlite_dataReader.GetInt64(1).ToString())
                        {
                            isExist = true;
                            break;
                        }

                    }
                    sqlite_dataReader.Close();
                    if (isExist == true)
                    {
                        returnString = "exist";

                    }
                    else
                    {
                        returnString = "notExist";
                    }
                }
                return returnString;
            }
            #region printReceipt
            private void PrintReceipt()
            {
                if (printList[0].Contains("holdPrint"))
                {
                    kitchenRecipt();
                }
                else if (printList[0].Contains("printReceipt"))
                {
                    receiptPrint();
                }
            }
            private void receiptPrint()
            {
                var printDocument = new PrintDocument();

                printDocument.PrintPage += new PrintPageEventHandler(PrintReceipt);
                PrinterSettings printerSettings = new PrinterSettings();
                printerSettings.PrinterName = printerName;
                printDocument.PrinterSettings = printerSettings;
                printDocument.Print();
            }

            private void PrintReceipt(object sender, PrintPageEventArgs t)
            {
                Graphics graphics = t.Graphics;
                Font font = new Font("Courier New", 12);

                float fontHeight = font.GetHeight();

                int startX = 0;
                int startY = 0;
                int offSet = 120;

                int mCount = 0;
                int sCount = 0;
                int cCount = 0;
                int dCount = 0;


                //e.PageSettings.PaperSize.Width = 50;

                Image newImage = Image.FromFile("YoTukTuk.png");

                graphics.DrawImage(newImage, 80, 0, 100, 100);

                graphics.DrawString("Yo Tuk Tuk \n 53 Lairgate \n Beverley \n HU17 8ET\n01482 881955", new Font("Calibri (Body)", 12), new SolidBrush(System.Drawing.Color.Black), 80, 0 + offSet);
                offSet += 100;
                graphics.DrawString("Table " + printList[1].ToString(), new Font("Calibri (Body)", 12), new SolidBrush(System.Drawing.Color.Black), 80, 0 + offSet);
                offSet += 40;
                for (int i = 0; i < printList.Count; i++)
                {
                    void shortMethod()
                    {
                        graphics.DrawString(printList[i + 1], new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                        graphics.DrawString(printList[i + 2], new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), 40, startY + offSet);
                        graphics.DrawString("£ " + printList[i + 3], new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), 200, startY + offSet);

                        offSet += 20;
                    }
                    switch (printList[i])
                    {
                        case "*s":
                            shortMethod();
                            break;
                        case "*m":
                            if (mCount == 0)
                            {
                                graphics.DrawString("--------------------------------------", new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                                offSet += 20;
                                mCount += 1;
                            }
                            shortMethod();
                            break;
                        case "*sd":
                            if (sCount == 0)
                            {
                                graphics.DrawString("--------------------------------------", new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                                offSet += 20;
                                sCount += 1;
                            }
                            shortMethod();
                            break;
                        case "*c":
                            if (cCount == 0)
                            {
                                graphics.DrawString("--------------------------------------", new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                                offSet += 20;
                                cCount += 1;
                            }
                            shortMethod();
                            break;
                        case "*d":
                            if (dCount == 0)
                            {
                                graphics.DrawString("--------------------------------------", new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                                offSet += 20;
                                dCount += 1;
                            }
                            shortMethod();
                            break;
                        case "membersDiscount":
                            offSet += 20;
                            graphics.DrawString("Members Discount", new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                            graphics.DrawString("£-" + printList[i + 1], new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), 200, startY + offSet);

                            offSet += 20;
                            break;
                        case "grandTotal":
                            graphics.DrawString("Grand Total", new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                            graphics.DrawString("£ " + printList[i + 1].ToString(), new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), 200, startY + offSet);
                            offSet += 20;
                            break;
                        case "paid":
                            graphics.DrawString("Paid", new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                            graphics.DrawString("£ " + printList[i + 1], new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), 200, startY + offSet);

                            offSet += 40;

                            graphics.DrawString("Thank You For Dining With Us!\n\n", new Font("Arial", 12), new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                            offSet += 40;
                            break;
                        default:
                            break;
                    }



                }
            }
            #endregion
            #region kitchen Print
            private void kitchenRecipt()
            {
                var printDocument = new PrintDocument();

                printDocument.PrintPage += new PrintPageEventHandler(kitchenPrinter);
                PrinterSettings printerSettings = new PrinterSettings();
                printerSettings.PrinterName = printerName;
                printDocument.PrinterSettings = printerSettings;
                printDocument.Print();


            }


            private void kitchenPrinter(object sender, PrintPageEventArgs e)
            {

                List<string> visitedStrings = new List<string>();

                bool visited = false;
                int count = 0;

                Graphics graphics = e.Graphics;
                Font font = new Font("Arial", 12);

                float fontHeight = font.GetHeight();

                int startX = 0;
                int startY = 0;
                int offSet = 20;

                int loop = 0;
                int sdLoop = 0;
                int dloop = 0;
                int cLoop = 0;

                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;


                graphics.DrawString("Table " + printList[1], font, new SolidBrush(System.Drawing.Color.Black), 100, 0 + 0);
                offSet += 20;

                for (int i = 0; i < printList.Count; i++)
                {
                    void shortMethod()
                    {
                        for (int z = 0; z < visitedStrings.Count; z++)
                        {
                            if (printList[i + 1] == visitedStrings[z])
                            {
                                visited = true;
                                break;
                            }
                        }
                        if (visited == false)
                        {
                            for (int x = 0; x < printList.Count; x++)
                            {
                                if (printList[i + 1] == printList[x])
                                {
                                    count++;
                                }
                            }

                            graphics.DrawString(count.ToString() + " x " + printList[i + 1], font, new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                            offSet += 20;

                            visitedStrings.Add(printList[i + 1]);

                        }
                        visited = false;
                        count = 0;
                    }

                    switch (printList[i])
                    {
                        case "*s":

                            shortMethod();
                            count = 0;
                            break;


                        case "*m":
                            {

                                if (loop == 0)
                                {

                                    graphics.DrawString("----------------------------", font, new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                                    offSet += 20;
                                }
                                loop++;
                                shortMethod();
                                break;
                            }
                        case "*sd":
                            {
                                if (sdLoop == 0)
                                {

                                    graphics.DrawString("----------------------------", font, new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                                    offSet += 20;
                                }
                                sdLoop++;
                                loop = 0;
                                shortMethod();
                                break;

                            }
                        case "*c":
                            {
                                if (cLoop == 0)
                                {

                                    graphics.DrawString("----------------------------", font, new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                                    offSet += 20;
                                }
                                cLoop++;
                                loop = 0;
                                shortMethod();
                                count = 0;
                                break;
                            }


                        case "*d":
                            {
                                if (dloop == 0)
                                {

                                    graphics.DrawString("----------------------------", font, new SolidBrush(System.Drawing.Color.Black), startX, startY + offSet);
                                    offSet += 20;
                                }
                                dloop++;
                                shortMethod();
                                visited = false;
                                count = 0;
                                break;
                            }
                        default:
                            break;


                    }
                }

            }

            #endregion
            #region readItems database
            private string readItems(string what)
            {
                string read = "";
                try
                {
                    SQLiteDataReader sqlite_dataReader;
                    SQLiteCommand sqlite_command;
                    sqlite_command = sqlConnection.CreateCommand();
                    if (what == "itemsList")
                    {
                        sqlite_command.CommandText = "SELECT Item, Price FROM ItemList";
                        sqlite_dataReader = sqlite_command.ExecuteReader();
                        while (sqlite_dataReader.Read())
                        {

                            read += sqlite_dataReader.GetString(0) + ",";
                            read += sqlite_dataReader.GetDouble(1) + ",";


                        }
                    }
                    else if (what == "listAll")
                    {
                        sqlite_command.CommandText = "SELECT * FROM ItemList";
                        sqlite_dataReader = sqlite_command.ExecuteReader();
                        while (sqlite_dataReader.Read())
                        {

                            read += sqlite_dataReader.GetInt16(0) + ",";
                            read += sqlite_dataReader.GetString(1) + ",";
                            read += sqlite_dataReader.GetString(2) + ",";
                            read += sqlite_dataReader.GetDouble(3) + ",";
                        }
                    }



                }
                catch (Exception e)
                {

                }
                return read;
            }
            #endregion
            #region database

            public void InsertData(string[] allText)
            {

                try
                {
                    SQLiteCommand sQLiteCommand;
                    sQLiteCommand = sqlConnection.CreateCommand();
                    if (allText[0].Contains("updateDetails"))
                    {
                        sQLiteCommand.CommandText = "UPDATE ItemList SET category = @category, Item = @item, Price = @price WHERE ItemID = @id";
                        sQLiteCommand.Parameters.AddWithValue("@category", allText[2]);
                        sQLiteCommand.Parameters.AddWithValue("@item", allText[3]);
                        sQLiteCommand.Parameters.AddWithValue("@price", allText[4]);
                        sQLiteCommand.Parameters.AddWithValue("@id", allText[1]);
                    }
                    else
                    {
                        sQLiteCommand.CommandText = "INSERT INTO PaidTable(OrderId, DataTime, TableNumber, Amount, DiscountAmount, PaymentMethod, Reset) values (@orderid, @datetime,@tablenumber,@amount,@discount,@paymentmethod,@reset)";
                        sQLiteCommand.Parameters.AddWithValue("@orderid", allText[2]);
                        sQLiteCommand.Parameters.AddWithValue("@datetime", allText[3]);
                        sQLiteCommand.Parameters.AddWithValue("@tablenumber", allText[4]);
                        sQLiteCommand.Parameters.AddWithValue("@amount", allText[5]);
                        sQLiteCommand.Parameters.AddWithValue("@discount", allText[6]);
                        sQLiteCommand.Parameters.AddWithValue("@paymentmethod", allText[7]);
                        sQLiteCommand.Parameters.AddWithValue("@reset", allText[8]);
                    }
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
            string userTable = "CREATE TABLE IF NOT EXISTS Users (Name TEXT, LogInId INT)";

            sQCommand = connection.CreateCommand();

            sQCommand.CommandText = orderTable;
            sQCommand.ExecuteNonQuery();

            sQCommand.CommandText = paidTable;
            sQCommand.ExecuteNonQuery();

            sQCommand.CommandText = userTable;
            sQCommand.ExecuteNonQuery();


        }

    }
    #endregion
}

