using System.Net.Sockets;
using Uplauncher.ArkanSound.Network;

namespace Uplauncher.ArkanSound.Game
{
    public class GameServer
    {
        #region Déclaration
        private readonly SimpleServer m_server;
        private GameClient m_client;
        private bool m_started;
        #endregion

        public GameServer()
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

            m_server.Start(8080);
            m_started = true;
        }

        #region Socket auth
        private void AccepteClient(Socket client)
        {
            var newClient = new SimpleClient(client);
            m_client = new GameClient(newClient);
            m_client.Disconnected += ClientDisconnected;
        }

        private void ClientDisconnected(object sender, GameClient.DisconnectedArgs e)
        {
            if (ReferenceEquals(m_client, e.Host))
            {
                m_client = null;
            }
        }
        #endregion

        public GameClient Client
        {
            get { return m_client; }
        }
    }
}
