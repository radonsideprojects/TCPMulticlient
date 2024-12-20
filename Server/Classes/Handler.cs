﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Classes.Serializable;

namespace Server.Classes
{
    internal class Handler
    {
        private Connection connection;

        private List<User> users;
        public void Initialize()
        {
            users = new List<User>();
            connection = new Connection();
            connection.onConnected += onClientConnected;
            connection.onDisconnected += onClientDisconnected;
            connection.onReceived += onDataReceived;
            connection.Listen();
        }

        private void onClientDisconnected(object sender, ConnectedArgs e)
        {
            Logging.Warning("Client disconnected: " + e.client.Client.RemoteEndPoint);

            User userToRemove = users.Find(u => u.Client == e.client);

            if (userToRemove != null)
            {
                users.Remove(userToRemove);
                Logging.Success($"Removed user {userToRemove.Username ?? "Unknown"} from the list.");

                Message message = new Message
                {
                    Type = "userLeft",
                    Username = userToRemove.Username
                };

                Message listMessage = new Message
                {
                    Type = "userList",
                    Username = "system",
                    Data = Serialization.SerializeListToBytes(users)
                };

                connection.broadcast(Serialization.XmlSerializeToByte(message));
                connection.broadcast(Serialization.XmlSerializeToByte(listMessage));
            }
        }

        private void onClientConnected(object sender, ConnectedArgs e)
        {
            Logging.Success("Client connected: " + e.client.Client.RemoteEndPoint);
        }

        private void onDataReceived(object sender, ReceivedArgs e)
        {
            Message message = new Message();
            User user = new User();
            try
            {
                message = Serialization.XmlDeserializeFromBytes<Message>(e.data);
                switch (message.Type)
                {
                    case "chatMessage":
                        if (string.IsNullOrWhiteSpace(Encoding.UTF8.GetString(message.Data)))
                        {
                            Logging.Warning("Received an empty message from: " + e.sender.Client.RemoteEndPoint + $" ({message.Username}), " + "ignoring.");
                            break;
                        }
                        Logging.Info("Received a chat message from: " + e.sender.Client.RemoteEndPoint + $" ({message.Username})");
                        connection.broadcast(e.data);
                        break;
                    case "userJoined":

                        Message takenMessage = new Message
                        {
                            Type = "userTaken",
                            Username = message.Username
                        };

                        if (users.Any(u => u.Username == message.Username))
                        {
                            connection.sendToClient(e.sender, Serialization.XmlSerializeToByte(takenMessage));
                            connection.disconnectClient(e.sender);
                            break;
                        }

                        user.Client = e.sender;
                        user.Username = message.Username;

                        users.Add(user);

                        Logging.Success("Added client " + $"{e.sender.Client.RemoteEndPoint} ({message.Username}) to the user list.");

                        Message serverMessage = new Message
                        {
                            Type = "userJoined",
                            Username = message.Username
                        };

                        Message listMessage = new Message
                        {
                            Type = "userList",
                            Username = "system",
                            Data = Serialization.SerializeListToBytes(users)
                        };

                        connection.broadcast(Serialization.XmlSerializeToByte(serverMessage));
                        connection.broadcast(Serialization.XmlSerializeToByte(listMessage));

                        break;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Error: " + ex.Message);
            }
        }
    }
}