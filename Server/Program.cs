﻿using System;
using System.Text;
using Server.Classes;
using System.Threading;
using System.Windows;

namespace Server
{
    internal class Program
    {
        private static Connection connection;
        public static void Initiate()
        {
            connection = new Connection();
            connection.onConnected += onClientConnection;
            connection.onReceived += onDataReceived;
            connection.Listen();
        }

        private static void onDataReceived(object sender, ReceivedArgs e)
        {
            Logging.Info("Received data from " + e.sender.Client.RemoteEndPoint + ": " + Encoding.UTF8.GetString(e.data));
        }

        private static void onClientConnection(object sender, ConnectedArgs e)
        {
            Logging.Success("Client connected: " + e.client.Client.RemoteEndPoint);
            connection.sendToClient(e.client, Encoding.UTF8.GetBytes("Test message! :P"));
        }

        [STAThread]
        public static void Main()
        {
            
            Initiate();
            Thread.Sleep(-1);
        }   
    }
}