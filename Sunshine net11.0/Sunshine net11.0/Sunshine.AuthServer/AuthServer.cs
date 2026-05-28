using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Sunshine.Logs;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.IO;
using Sunshine.AuthServer.Client;
using Sunshine.Protocol.Utils;
using Sunshine.Servers;
using Sunshine.BaseClient;
using Sunshine.BaseServer.Configuration;

namespace Sunshine.AuthServer
{
    public class AuthServer
    {
        #region Socket
        public static string Ip => GameConfig.GetString("AuthIp", "86.107.197.24");
        public static string BindIp => GameConfig.GetString("AuthBindIp", Ip);
        public static int Port => GameConfig.GetInt("AuthPort", 446);
        private Socket m_authserver;
        private Socket m_client;
        public Dictionary<Socket, IBaseClient> authClients = new Dictionary<Socket, IBaseClient>();
        private IPEndPoint Auth_Address
        {
            get { return ListenAddressResolver.CreateIPv4Endpoint(BindIp, Port, "AuthBindIp/AuthIp"); }
        }
        #endregion

        public AuthServer()
        {
            m_authserver = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Initialize()
        {
            Bind();
            Listen(100);
            Accept();
        }

        private void Bind()
        {
            m_authserver.Bind(Auth_Address);
        }

        private void Listen(int file)
        {
            m_authserver.Listen(file);
            string localAddress = ((IPEndPoint)m_authserver.LocalEndPoint).Address.ToString();
            Logger.WriteInfo(string.Format("Starting IPC Auth {0}:{1} (announced as {2}:{3})", localAddress, Port, Ip, Port));
        }

        private void Accept()
        {
            m_authserver.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }

        private void AcceptCallBack(IAsyncResult result)
        {
            AuthClient authClient = new AuthClient(m_client = m_authserver.EndAccept(result));
            try
            {
                ServersManager.Instance.AddClient(m_client, authClient);
                var protocolVersion = GameConfig.GetInt("ProtocolVersion", 1375);
                authClient.Send(new ProtocolRequired(protocolVersion, protocolVersion));
                authClient.Send(new HelloConnectMessage(1, authClient.Ticket = Utils.RandomString(32, true)));
                authClient.Initialize();
                Accept();
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
                authClient.Disconnect();
            }
        }
    }
}
