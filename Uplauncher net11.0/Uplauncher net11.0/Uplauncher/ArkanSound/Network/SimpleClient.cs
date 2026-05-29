using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Uplauncher.ArkanSound.Network
{
    public class SimpleClient
    {

        #region Variables

        public Socket Socket;
        public bool Runing { get; private set; }
        private byte[] sendBuffer, receiveBuffer;
        private byte[] buffer;
        const int bufferLength = 8192;
        public bool Normaly = false;
        #endregion

        #region Builder

        public SimpleClient(string ip, short port)
        {
            Init();
            Start(ip, port);
        }

        public SimpleClient(Socket socket)
        {
            Init();
            Start(socket);
        }

        public SimpleClient()
        {
            Init();
        }

        #endregion

        #region Methods

        public void Start(Socket socket)
        {
            try
            {
                Runing = true;
                Socket = socket;
                Socket.BeginReceive(receiveBuffer, 0, bufferLength, SocketFlags.None, new AsyncCallback(ReceiveCallBack), Socket);
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        public void Start(string ip, short port)
        {
            try
            {
                Socket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), port), new AsyncCallback(ConnectionCallBack), Socket);
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        public void Start(IPEndPoint endPoint)
        {
            try
            {
                Socket.BeginConnect(endPoint, new AsyncCallback(ConnectionCallBack), Socket);
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        public void Stop()
        {
            try
            {
                if (Socket.Connected == true)
                    Socket.BeginDisconnect(false, DisconectedCallBack, Socket);
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                if (Socket.Connected == false)
                    Runing = false;
                if (Runing)
                {
                    if (data.Length == 0)
                        return;
                    sendBuffer = data;
                    Socket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, new AsyncCallback(SendCallBack), Socket);
                }
                else
                    Console.WriteLine("Send " + data.Length + " bytes but not runing");
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        public void Dispose()
        {
            // Dispose

            if (Socket != null)
                Socket.Dispose();

            if (buffer != null)
                buffer = null;

            // Clean

            Socket = null;
            sendBuffer = null;
            receiveBuffer = null;
            buffer = null;
        }

        #endregion

        #region Private Methods

        private void Init()
        {
            try
            {
                buffer = new byte[8192];
                receiveBuffer = new byte[bufferLength];
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        #endregion

        #region CallBack

        private void ConnectionCallBack(IAsyncResult asyncResult)
        {
            try
            {
                Runing = true;
                Socket client = (Socket)asyncResult.AsyncState;
                client.EndConnect(asyncResult);
                client.BeginReceive(receiveBuffer, 0, bufferLength, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
                OnConnected(new ConnectedEventArgs());
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        private void DisconectedCallBack(IAsyncResult asyncResult)
        {
            try
            {
                Runing = false;
                Socket client = (Socket)asyncResult.AsyncState;
                client.EndDisconnect(asyncResult);
                OnDisconnected(new DisconnectedEventArgs(this));
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        private void ReceiveCallBack(IAsyncResult asyncResult)
        {
            Socket client = (Socket)asyncResult.AsyncState;
            if (client.Connected == false)
            {
                Runing = false;
                return;
            }
            if (Runing)
            {
                int bytesRead = 0;

                try
                {
                    bytesRead = client.EndReceive(asyncResult);
                }
                catch (System.Exception ex)
                {
                    OnError(new ErrorEventArgs(ex));
                }

                if (bytesRead == 0)
                {
                    Runing = false;
                    OnDisconnected(new DisconnectedEventArgs(this));
                    return;
                }
                byte[] data = new byte[bytesRead];
                Array.Copy(receiveBuffer, data, bytesRead);
                buffer = data;
                OndataReceived(new dataReceivedEventArgs(buffer));
                try
                {
                    client.BeginReceive(receiveBuffer, 0, bufferLength, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
                }
                catch (System.Exception ex)
                {
                    OnError(new ErrorEventArgs(ex));
                }
            }
            else
                Console.WriteLine("Receive data but not running");
        }

        private void SendCallBack(IAsyncResult asyncResult)
        {
            try
            {
                if (Runing == true)
                {
                    Socket client = (Socket)asyncResult.AsyncState;
                    client.EndSend(asyncResult);
                    OndataSended(new dataSendedEventArgs());
                }
                else
                    Console.WriteLine("Send data but not runing !");
            }
            catch (System.Exception ex)
            {
                OnError(new ErrorEventArgs(ex));
            }
        }

        #endregion

        #region Events

        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<dataReceivedEventArgs> dataReceived;
        public event EventHandler<dataSendedEventArgs> dataSended;
        public event EventHandler<ErrorEventArgs> Error;

        private void OnConnected(ConnectedEventArgs e)
        {
            if (Connected != null)
                Connected(this, e);
        }

        private void OnDisconnected(DisconnectedEventArgs e)
        {
            if (Disconnected != null)
                Disconnected(this, e);
        }

        private void OndataReceived(dataReceivedEventArgs e)
        {
            if (dataReceived != null)
                dataReceived(this, e);
        }

        private void OndataSended(dataSendedEventArgs e)
        {
            if (dataSended != null)
                dataSended(this, e);
        }

        private void OnError(ErrorEventArgs e)
        {
            if (Error != null)
                Error(this, e);
        }

        #endregion

        #region EventArgs

        public class ConnectedEventArgs : EventArgs
        {
        }

        public class DisconnectedEventArgs : EventArgs
        {
            public SimpleClient Socket { get; private set; }

            public DisconnectedEventArgs(SimpleClient socket)
            {
                Socket = socket;
            }
        }

        public class dataSendedEventArgs : EventArgs
        {
        }

        public class dataReceivedEventArgs : EventArgs
        {
            public byte[] data { get; private set; }

            public dataReceivedEventArgs(byte[] Data)
            {
                data = Data;
            }
        }

        public class ErrorEventArgs : EventArgs
        {
            public System.Exception Ex { get; private set; }

            public ErrorEventArgs(System.Exception ex)
            {
                Ex = ex;
            }
        }

        #endregion

    }
}
