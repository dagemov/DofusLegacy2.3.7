using Sunshine.BaseClient;
using Sunshine.BaseServer.Configuration;
using Sunshine.Logs;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Messages;
using Sunshine.Servers;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer
{
    public class WorldServer
    {
        #region World Config
        public static string Ip => GameConfig.GetString("WorldIp", "86.107.197.24");
        public static string BindIp => GameConfig.GetString("WorldBindIp", Ip);
        public static int Port => GameConfig.GetInt("WorldPort", 3467);
        private Socket m_worldserver;
        private Socket m_client;
        public Dictionary<Socket, IBaseClient> worldClients = new Dictionary<Socket, IBaseClient>();
        private IPEndPoint World_Address
        {
            get { return ListenAddressResolver.CreateIPv4Endpoint(BindIp, Port, "WorldBindIp/WorldIp"); }
        }
        #endregion

        public WorldServer()
        {
            m_worldserver = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Initialize()
        {
            Bind();
            Listen(100);
            Accept();
        }

        private void Bind()
        {
            m_worldserver.Bind(World_Address);
        }

        private void Listen(int file)
        {
            m_worldserver.Listen(file);
            string localAddress = ((IPEndPoint)m_worldserver.LocalEndPoint).Address.ToString();
            Logger.WriteInfo(string.Format("Starting IPC World {0}:{1} (announced as {2}:{3})", localAddress, Port, Ip, Port));
        }

        private void Accept()
        {
            m_worldserver.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }       

        private void AcceptCallBack(IAsyncResult result)
        {
            WorldClient worldClient = new WorldClient(m_client = m_worldserver.EndAccept(result));
            try
            {
                ServersManager.Instance.AddClient(m_client, worldClient);
                var protocolVersion = GameConfig.GetInt("ProtocolVersion", 1375);
                worldClient.Send(new ProtocolRequired(protocolVersion, protocolVersion));
                worldClient.Send(new HelloGameMessage());
                worldClient.Initialize();
                Accept();
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
                worldClient.Disconnect();
                return;
            }
        }
    }
}
