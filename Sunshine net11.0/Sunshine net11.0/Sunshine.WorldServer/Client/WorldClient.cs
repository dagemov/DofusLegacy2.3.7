using Sunshine.BaseClient;
using Sunshine.Logs;
using Sunshine.MySql.Database.Auth;
using Sunshine.MySql.Database.Auth.Accounts;
using Sunshine.MySql.Database.Auth.Worlds;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Messages;
using Sunshine.Servers;
using Sunshine.WorldServer.Game;
using Sunshine.WorldServer.Game.BidsHouse;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Sunshine.WorldServer.Client
{
    public class WorldClient : IBaseClient
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
            StartPendingTokensMonitor();
            _client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), _client);
        }

        private int _disconnecting = 0;
        private Timer _pendingTokensTimer;
        private int _pendingTokensSyncing = 0;

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnecting, 1) == 1)
                return;

            try
            {
                Handlers.Mounts.MountHandler.ClearPublicPaddockMountVisuals(this, true);
            }
            catch
            {
            }

            try
            {
                if (Character != null && Character.Client == this)
                    Character.LogOut();
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex.ToString());
            }

            try
            {
                StopPendingTokensMonitor();

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

        private void StartPendingTokensMonitor()
        {
            if (_pendingTokensTimer != null)
                return;

            _pendingTokensTimer = new Timer(CheckPendingTokens, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        }

        private void StopPendingTokensMonitor()
        {
            var timer = Interlocked.Exchange(ref _pendingTokensTimer, null);
            timer?.Dispose();
        }

        private void CheckPendingTokens(object state)
        {
            if (Interlocked.Exchange(ref _pendingTokensSyncing, 1) == 1)
                return;

            try
            {
                if (Account == null || Character == null || Character.Client != this || Character.Inventory == null)
                    return;

                int receivedTokens = Character.Inventory.MergePendingTokens(true);
                if (receivedTokens > 0)
                    Character.SendServerMessage($"Vous avez reçu {receivedTokens} Pièce(s) de Kama Géante(s).");
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref _pendingTokensSyncing, 0);
            }
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
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
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

        public void Send(Message message)
        {
            if (message == null || _client == null || Interlocked.CompareExchange(ref _disconnecting, 0, 0) == 1)
                return;

            try
            {
                if (!_client.Connected)
                    return;

                using (BigEndianWriter writer = new BigEndianWriter())
                {
                    message.Pack(writer);

                    if (writer.Data == null || writer.Data.Length == 0)
                        return;

                    _client.BeginSend(writer.Data, 0, writer.Data.Length, SocketFlags.None, new AsyncCallback(SendCallBack), _client);
                    Logger.WriteServer("WorldServer", WorldServer.Ip, WorldServer.Port, message.ToString());
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                Disconnect();
                return;
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
                Disconnect();
                return;
            }
        }

        private bool Connected()
        {
            if ((_client != null && _client.Connected))
            {
                try
                {
                    if ((_client.Poll(0, SelectMode.SelectRead)))
                    {
                        if (_client.Receive(new byte[1], SocketFlags.Peek) == 0)
                        {
                            return false;
                        }
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

        public WorldClient(Socket client)
        {
            _client = client;
            Ip = ((IPEndPoint)_client.RemoteEndPoint).Address.ToString();
            Port = ((IPEndPoint)_client.RemoteEndPoint).Port;
        }

        public World World { get; set; }

        public Account Account { get; set; }

        public CharacterRecord[] Characters
            => AccountManager.Instance.GetCharacters(Account.Id);

        public Character Character { get; set; }

        public HashSet<string> SessionIgnoredNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int? LastCharacter { get; set; }
    }
}
