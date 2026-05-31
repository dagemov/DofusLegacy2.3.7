using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uplauncher.ArkanSound.Network;

namespace Uplauncher.ArkanSound.Reg
{
    public class RegClient
    {
        #region Déclaration
        private SimpleClient m_client;

        public event EventHandler<DisconnectedArgs> Disconnected;
        #endregion

        public RegClient(SimpleClient client)
        {
            m_client = client;

            if (client != null)
            {
                m_client.dataReceived += this.ClientdataReceive;
                m_client.Disconnected += this.ClientDisconnected;
            }
        }

        /// <summary>
        /// Permet de déconnecter le client
        /// </summary>
        public void Dipose()
        {
            m_client.dataReceived -= ClientdataReceive;
            m_client.Disconnected -= this.ClientDisconnected;

            m_client.Stop();
        }

        #region Events
        private void ClientdataReceive(object sender, SimpleClient.dataReceivedEventArgs e)
        {
            if (Initialization.gameServer != null && Initialization.gameServer.Client != null)
            {
                Initialization.gameServer.Client.Send(e.data);
            }
        }

        private void ClientDisconnected(object sender, SimpleClient.DisconnectedEventArgs e)
        {
            OnDisconnected(new DisconnectedArgs(this));
        }
        private void OnDisconnected(DisconnectedArgs e)
        {
            if (Disconnected != null)
                Disconnected(this, e);
        }
        #endregion
        public class DisconnectedArgs : EventArgs
        {
            public RegClient Host { get; private set; }

            public DisconnectedArgs(RegClient host)
            {
                Host = host;
            }
        }

        public void Send(byte[] data)
        {
            if (m_client.Runing)
                m_client.Send(data);
        }
    }
}
