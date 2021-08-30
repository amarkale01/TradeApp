using System;
using System.Collections.Generic;
using System.Text;

namespace AliceBlueWrapper
{
    public interface IWebSocket
    {
        event OnConnectHandler OnConnect;
        event OnCloseHandler OnClose;
        event OnDataHandler OnData;
        event OnErrorHandler OnError;
        bool IsConnected();
        void Connect(string Url);
        void Send(string Message);
        void Close(bool Abort = false);
    }
}
