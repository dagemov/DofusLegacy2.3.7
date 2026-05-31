using System.Net.Sockets;
using Uplauncher.ArkanSound.Network;

namespace Uplauncher.ArkanSound.Reg
{
    public class RegServer
    {
        #region Déclaration
        private readonly SimpleServer m_server;
        private RegClient m_client;
        private bool m_started;
        #endregion

        public RegServer()
        {
            m_server = new SimpleServer();
            m_server.ConnectionAccepted += AccepteClient;
        }

        public void StartAuthentificate()
        {
            if (m_started)
            {
                return;
            }

            m_server.Start(8081);
            m_started = true;
        }

        #region Socket auth
        private void AccepteClient(Socket client)
        {
            var newClient = new SimpleClient(client);
            m_client = new RegClient(newClient);
            m_client.Disconnected += ClientDisconnected;
        }

        private void ClientDisconnected(object sender, RegClient.DisconnectedArgs e)
        {
            if (ReferenceEquals(m_client, e.Host))
            {
                m_client = null;
            }
        }
        #endregion

        public RegClient Client
        {
            get { return m_client; }
        }
    }
}
