using Sunshine.AuthServer.Handlers;
using Sunshine.BaseClient;
using Sunshine.Logs;
using Sunshine.MySql.Database.Auth;
using Sunshine.MySql.Database.Auth.Accounts;
using Sunshine.MySql.Database.Auth.Worlds;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Messages;
using Sunshine.Servers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.AuthServer.Client
{
    public class AuthClient : IBaseClient
    {
        #region Socket
        private const int MaxPacketLength = 16 * 1024 * 1024;

        private Socket _client;
        private readonly byte[] _buffer = new byte[8192];
        private readonly List<byte> _pendingData = new List<byte>(8192);
        public string Ip { get; set; }
        public int Port { get; set; }

        public void Initialize()
        {
            _client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), _client);
        }

        public void Disconnect()
        {
            try
            {
                if (_client != null)
                {
                    if (_client.Connected)
                        _client.Shutdown(SocketShutdown.Both);

                    _client.Close();
                }
            }
            catch
            {
            }

            ServersManager.Instance.RemoveClient(this);
        }

        public void DisconnectLater(int delayMs = 1200)
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs);
                }
                catch
                {
                }

                Disconnect();
            });
        }

        private void ReceiveCallBack(IAsyncResult result)
        {
            try
            {
                _client = (Socket)result.AsyncState;

                if (!this.Connected())
                {
                    this.Disconnect();
                    return;
                }

                int size = _client.EndReceive(result);
                if (size <= 0)
                {
                    Disconnect();
                    return;
                }

                AppendReceivedBytes(size);
                ProcessPendingMessages();
                _client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), _client);
            }
            catch (Exception)
            {
                Disconnect();
                return;
            }
        }

        private void AppendReceivedBytes(int count)
        {
            for (int i = 0; i < count; i++)
                _pendingData.Add(_buffer[i]);
        }

        private void ProcessPendingMessages()
        {
            byte[] packet;
            while (TryDequeuePacket(out packet))
            {
                MessageDispatcher.Dispatch(this, packet);
            }
        }

        private bool TryDequeuePacket(out byte[] packet)
        {
            packet = null;

            if (_pendingData.Count < 2)
                return false;

            int header = (_pendingData[0] << 8) | _pendingData[1];
            int lengthBytesCount = header & 0x03;
            int totalHeaderLength = 2 + lengthBytesCount;

            if (_pendingData.Count < totalHeaderLength)
                return false;

            int payloadLength = 0;
            switch (lengthBytesCount)
            {
                case 0:
                    payloadLength = 0;
                    break;
                case 1:
                    payloadLength = _pendingData[2];
                    break;
                case 2:
                    payloadLength = (_pendingData[2] << 8) | _pendingData[3];
                    break;
                case 3:
                    payloadLength = (_pendingData[2] << 16) | (_pendingData[3] << 8) | _pendingData[4];
                    break;
                default:
                    Logger.WriteError(string.Format("Malformed packet header received from {0}:{1}.", Ip, Port));
                    Disconnect();
                    return false;
            }

            if (payloadLength < 0 || payloadLength > MaxPacketLength)
            {
                Logger.WriteError(string.Format("Invalid packet length received from {0}:{1} => {2}", Ip, Port, payloadLength));
                Disconnect();
                return false;
            }

            int packetLength = totalHeaderLength + payloadLength;
            if (_pendingData.Count < packetLength)
                return false;

            packet = _pendingData.GetRange(0, packetLength).ToArray();
            _pendingData.RemoveRange(0, packetLength);
            return true;
        }

        public void Send(Message message)
        {
            try
            {
                using (BigEndianWriter writer = new BigEndianWriter())
                {
                    message.Pack(writer);
                    byte[] data = writer.Data;
                    _client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallBack), _client);
                    Logger.WriteServer("AuthServer", AuthServer.Ip, AuthServer.Port, message.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
                Disconnect();
            }
        }

        public void SendAndDisconnect(Message message, int delayMs = 1200)
        {
            try
            {
                if (message != null && _client != null && _client.Connected)
                {
                    using (BigEndianWriter writer = new BigEndianWriter())
                    {
                        message.Pack(writer);
                        byte[] data = writer.Data;
                        int sent = 0;

                        while (sent < data.Length)
                        {
                            int count = _client.Send(data, sent, data.Length - sent, SocketFlags.None);
                            if (count <= 0)
                                break;

                            sent += count;
                        }

                        Logger.WriteServer("AuthServer", AuthServer.Ip, AuthServer.Port, message.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
            }

            DisconnectLater(delayMs);
        }

        private void SendCallBack(IAsyncResult result)
        {
            try
            {
                _client = (Socket)result.AsyncState;
                _client.EndSend(result);
            }
            catch (Exception)
            {
                return;
            }
        }

        private bool Connected()
        {
            if ((_client != null && _client.Connected))
            {
                try
                {
                    if (_client.Poll(0, SelectMode.SelectRead))
                    {
                        if (_client.Receive(new byte[1], SocketFlags.Peek) == 0)
                            return false;
                    }
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
            }
            else
                return false;
        }
        #endregion

        public AuthClient(Socket client)
        {
            _client = client;
            Ip = ((IPEndPoint)_client.RemoteEndPoint).Address.ToString();
            Port = ((IPEndPoint)_client.RemoteEndPoint).Port;
        }

        public string Ticket { get; set; }

        public Account Account { get; set; }
    }
}
