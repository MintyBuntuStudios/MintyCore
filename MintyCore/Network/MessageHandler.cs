﻿using System.Collections.Generic;
using System.Linq;
using ENet;
using MintyCore.Utils;
using MintyCore.Utils.Maths;

namespace MintyCore.Network
{
    public class MessageHandler
    {
        private static readonly Dictionary<Identification, IMessage> _messages = new();
        private static readonly HashSet<MessageHandler> _instances = new();

        private readonly Server? _server;
        private readonly Client? _client;

        internal MessageHandler(Server server)
        {
            _server = server;
            _instances.Add(this);
        }

        internal MessageHandler(Client client)
        {
            _client = client;
            _instances.Add(this);
        }

        public static void SendAutoMessages()
        {
            foreach (var instance in _instances)
            {
                instance.SendAutoMessagesInternal();
            }
        }

        private void SendAutoMessagesInternal()
        {
            foreach (var (key, message) in _messages)
            {
                if(!message.AutoSend || Engine.Tick % message.AutoSendInterval != 0) continue;
                SendMessage(key);
            }
        }

        internal static void AddMessage<TMessage>(Identification messageId) where TMessage : class, IMessage, new()
        {
            _messages.Add(messageId, new TMessage());
        }
        
        public static void Clear()
        {
            _messages.Clear();
            _instances.Clear();
        }

        public void SendMessage(Identification messageId, object? data = null)
        {
            var message = _messages[messageId];
            message.PopulateMessage(data);

            message.IsServer = _server is not null;

            DataWriter writer = new DataWriter();

            writer.Put((int)MessageType.REGISTERED_MESSAGE);
            messageId.Serialize(writer);
            message.Serialize(writer);

            Packet packet = default;
            packet.Create(writer.Buffer, (PacketFlags)message.DeliveryMethod);

            if (_server is not null)
            {
                //Send to all players, if no player is specified send to all
                foreach (var receiver in (message.Receivers is not null && message.Receivers.Length != 0) ? message.Receivers : Engine.PlayerIDs.Keys.ToArray())
                {
                    _server.SendMessage(receiver, packet, message.DeliveryMethod);
                }
            }

            if (_client is not null)
            {
                _client.SendMessage(packet, message.DeliveryMethod);
            }
        }

        public void HandleMessage(DataReader reader)
        {
            var id = Identification.Deserialize(reader);

            var message = _messages[id];
            
            message.IsServer = _server is not null;

            //Check if the message is allowed to be  received
            if (_server is not null &&
                !MathHelper.IsBitSet((int)message.MessageDirection, (int)MessageDirection.CLIENT_TO_SERVER)) return;
            if (_client is not null &&
                !MathHelper.IsBitSet((int)message.MessageDirection, (int)MessageDirection.SERVER_TO_CLIENT)) return;

            message.Deserialize(reader);
            message.Clear();
        }

 
    }
}