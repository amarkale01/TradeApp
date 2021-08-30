using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AliceBlueWrapper
{
    public delegate void OnConnectHandler();
    public delegate void OnCloseHandler();
    public delegate void OnErrorHandler(string errorMessage);
    public delegate void OnDataHandler(byte[] data, int count, string messageType);

    public class WebSocket: IWebSocket
    {
        ClientWebSocket socket;
        string socketUrl;
        //int _bufferLength = 999999999;
        int _bufferLength = 1000000;
        public event OnConnectHandler OnConnect;
        public event OnCloseHandler OnClose;
        public event OnDataHandler OnData;
        public event OnErrorHandler OnError;
        

        public WebSocket()
        {
            
        }

        public bool IsConnected()
        {
            if (socket == null)
                return false;
            return socket.State == WebSocketState.Open;
        }

        public void Connect(string url)
        {
            socketUrl = url;
            try
            {
                socket = new ClientWebSocket();
                socket.ConnectAsync(new Uri(socketUrl), CancellationToken.None).Wait();
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    string errMessage = exception.Message;
                    OnError?.Invoke("Error while connecting: Message : " + errMessage);
                    if (errMessage.Contains("Forbidden") && errMessage.Contains("403"))
                    {
                        OnClose?.Invoke();
                    }
                }
                return;
            }
            catch (Exception e)
            {
                OnError?.Invoke("Error while connecting. Message:  " + e.Message);
                return;
            }
            OnConnect?.Invoke();

            OnError?.Invoke("Connected to feeds API...");

            byte[] buffer = new byte[_bufferLength];

            Action<Task<WebSocketReceiveResult>> callback = null;

            try
            {
                callback = t =>
                 {
                     try
                     {
                         byte[] tempBuff = new byte[_bufferLength];
                         int offset = t.Result.Count;
                         bool endOfMessage = t.Result.EndOfMessage;
                         // if chunk has even more data yet to recieve do that synchronously
                         while (!endOfMessage)
                         {
                             WebSocketReceiveResult result = socket.ReceiveAsync(new ArraySegment<byte>(tempBuff), CancellationToken.None).Result;
                             Array.Copy(tempBuff, 0, buffer, offset, result.Count);
                             offset += result.Count;
                             endOfMessage = result.EndOfMessage;
                         }
                         // send data to process
                         OnData?.Invoke(buffer, offset, t.Result.MessageType.ToString());
                         // Again try to receive data
                         socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ContinueWith(callback);
                     }
                     catch (Exception e)
                     {
                         if (IsConnected())
                             OnError?.Invoke("Error while recieving data. Message:  " + e.Message);
                         else
                             OnError?.Invoke("Lost ticker connection.");
                     }
                 };

                // To start the receive loop in the beginning
                socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ContinueWith(callback);
            }
            catch (Exception e)
            {
                OnError?.Invoke("Error while recieving data. Message:  " + e.Message);
            }
        }
        /// <summary>
        /// Send message to socket connection
        /// </summary>
        /// <param name="Message">Message to send</param>
        public void Send(string Message)
        {
            if (socket.State == WebSocketState.Open)
                try
                {
                    socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(Message)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                }
                catch (Exception e)
                {
                    OnError?.Invoke("Error while sending data. Message:  " + e.Message);
                }
        }

        /// <summary>
        /// Close the WebSocket connection
        /// </summary>
        /// <param name="Abort">If true WebSocket will not send 'Close' signal to server. Used when connection is disconnected due to netork issues.</param>
        public void Close(bool Abort = false)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    if (Abort)
                        socket.Abort();
                    else
                    {
                        socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
                        OnClose?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    OnError?.Invoke("Error while closing connection. Message: " + e.Message);
                }
            }
        }


        

    }
}
