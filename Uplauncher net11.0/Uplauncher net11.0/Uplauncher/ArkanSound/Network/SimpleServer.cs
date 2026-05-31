using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Uplauncher.ArkanSound.Network
{
    public class SimpleServer
    {

        #region Variables

        private Socket socketListener;
        private bool runing = false;

        public bool Connected
        {
            get { return runing; }
        }
        public SimpleServer()
        {
            socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        }

        #endregion

        #region Methods

        public void Start(short listenPort)
        {
            if (runing)
            {
                return;
            }

            try
            {
                socketListener.Bind(new IPEndPoint(IPAddress.Any, listenPort));
                socketListener.Listen(5);
                runing = true;
                socketListener.BeginAccept(BeiginAcceptCallBack, socketListener);
            }
            catch
            {
                runing = false;
                throw;
            }
        }

        public void Stop()
        {
            runing = false;
            socketListener.Shutdown(SocketShutdown.Both);
        }

        private void BeiginAcceptCallBack(IAsyncResult result)
        {
            if (runing)
            {
                Socket listener = (Socket)result.AsyncState;
                Socket acceptedSocket = listener.EndAccept(result);
                OnConnectionAccepted(acceptedSocket);
                socketListener.BeginAccept(BeiginAcceptCallBack, socketListener);
            }
        }

        #endregion

        #region Events

        public delegate void ConnectionAcceptedDelegate(Socket acceptedSocket);
        public event ConnectionAcceptedDelegate ConnectionAccepted;
        private void OnConnectionAccepted(Socket client)
        {
            if (ConnectionAccepted != null)
                ConnectionAccepted(client);
        }

        #endregion

    }
}
