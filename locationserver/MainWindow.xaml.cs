using System;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.Windows;

namespace locationserver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    
    public partial class MainWindow : Window
    {
        bool runServer = false;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(runServer)
            {
                return;
            }
            new Thread(Program.runServer).Start();
            view_button.Text="Server is Listening";
            runServer = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if(!runServer)
            {
                return;
            }
            Program.listener.Stop();
            view_button.Text="Server Stopped Listening";
            runServer = false;
        }
    }
}