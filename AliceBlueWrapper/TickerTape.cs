using AliceBlueWrapper.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace AliceBlueWrapper
{
    public delegate void OnTickerTick(Tick tick);
    public class TickerTape : IDisposable
    {

        private IWebSocket _ws;
        private string token;
        private readonly string base_url = "wss://ant.aliceblueonline.com/hydrasocket/v2/websocket?access_token={0}";
        bool IsConnected = false;
        public Tick currentTick = null;
        public string errorMessage = string.Empty;
        public event OnErrorHandler OnError;
        public event OnTickerTick OnTickerTick;
        public List<Tick> subscribeTicks = null;

        Timer heartBeatTimer;

        public TickerTape(string accessToken,List<Tick> ticks)
        {
            token = accessToken;
            subscribeTicks = ticks;
            if (_ws == null)
                _ws = new WebSocket();

            _ws.OnConnect += _ws_OnConnect;
            _ws.OnData += _ws_OnData;
            _ws.OnClose += _ws_OnClose;
            _ws.OnError += _ws_OnError;

            string url = string.Format(base_url,token);
            _ws.Connect(url);


            //// initializing  watchdog timer
            //_timer = new System.Timers.Timer();
            //_timer.Elapsed += _onTimerTick;
            //_timer.Interval = 1000; // checks connection every second

            //if (IsConnected)
            //{
            //    UInt32[] Tokens = new UInt32[2];

            //    Subscribe(Tokens);
            //}

            if (heartBeatTimer == null)
            {
                heartBeatTimer = new System.Timers.Timer
                {
                    Interval = 5000
                };
            }
        }

        private void _ws_OnError(string errorMessage)
        {
            OnError?.Invoke(errorMessage);
        }

        private void _ws_OnClose()
        {
            
        }

        private void _ws_OnData(byte[] data, int count, string messageType)
        {
            short exchange = BitConverter.ToInt16(data, 1);

            int exchange1 = BitConverter.ToInt32(data, 3);
            int offset = 2;
            UInt32 val = ReadInt(data, ref offset);
            offset = 6;
            UInt32 ltp = ReadInt(data, ref offset) / 100;
            offset = 66;
            UInt32 open = ReadInt(data, ref offset) / 100;
            offset = 74;
            UInt32 close = ReadInt(data, ref offset) / 100;
            
            try
            {
                currentTick = new Tick();
                currentTick.LastPrice = ltp;
                currentTick.InstrumentToken = val;

                OnTickerTick?.Invoke(currentTick);
            }
            catch (Exception)
            {

            }
        }

        private void _ws_OnConnect()
        {
            IsConnected = true;

            UInt32[] Tokens = new UInt32[2];

            //Subscribe(Tokens);
            Subscribe(subscribeTicks);
        }


        /// <summary>
        /// Reads 4 byte int32 from byte stream
        /// </summary>
        private UInt32 ReadInt(byte[] b, ref int offset)
        {
            UInt32 data = (UInt32)BitConverter.ToUInt32(new byte[] { b[offset + 3], b[offset + 2], b[offset + 1], b[offset + 0] }, 0);
            offset += 4;
            return data;
        }

        /// <summary>
        /// Subscribe to a list of instrument_tokens.
        /// </summary>
        /// <param name="Tokens">List of instrument instrument_tokens to subscribe</param>
        public void Subscribe(UInt32[] Tokens)
        {
            if (Tokens.Length == 0) return;
            uint t1 = Tokens[0];
            t1 = 42231;


            string msg = "{\"a\":\"subscribe\",\"v\":[" + String.Join(",", Tokens) + "]}";
           
            //msg = "{\"a\": \"subscribe\", \"v\": [[1, 26000]], \"m\": \"marketdata\"}";//working nifty

            msg = "{\"a\": \"subscribe\", \"v\": [[1, 26009],[2, 43341]], \"m\": \"marketdata\"}";//working bank nifty

           // msg = "{\"a\": \"subscribe\", \"v\": [[4, 219002]], \"m\": \"marketdata\"}";//working silverm

            if (IsConnected)
            {
                _ws.Send(msg);
                HeartbeatTimerStart();
            }
           
        }

        public void Subscribe(List<Tick> Tokens)
        {
            if (Tokens.Count == 0) return;
            List<string> strTokens = new List<string>();

            foreach (Tick item in Tokens)
            {
                strTokens.Add(string.Format("[{0},{1}]",item.Mode,item.InstrumentToken));
            }

           //string msg = "{\"a\":\"subscribe\",\"v\":[" + String.Join(",", strTokens) + "]}";
            string msg = "{\"a\": \"subscribe\", \"v\": [" + String.Join(",", strTokens) + "], \"m\": \"marketdata\"}";//working nifty

            //string msg1 = "{\"a\": \"subscribe\", \"v\": [[1, 26000]], \"m\": \"marketdata\"}";//working nifty

            //msg = "{\"a\": \"subscribe\", \"v\": [[1, 26009],[2, 43341]], \"m\": \"marketdata\"}";//working bank nifty

            //msg = "{\"a\": \"subscribe\", \"v\": [[4, 219002]], \"m\": \"marketdata\"}";//working silverm

            if (IsConnected)
            {
                _ws.Send(msg);
                HeartbeatTimerStart();
            }

        }

        public void Dispose()
        {
            if (_ws != null && IsConnected)
            {
                _ws.Close();
                IsConnected = false;
                _ws = null;
            }

            if(heartBeatTimer != null)
                heartBeatTimer.Stop();
        }

        public void HeartbeatTimerStart()
        {
            heartBeatTimer = new System.Timers.Timer
            {
                Interval = 5000
            };

            heartBeatTimer.Elapsed += new ElapsedEventHandler(Heartbeat_Timer_Elapsed);
            //System.Threading.Thread.Sleep(2000);
            heartBeatTimer.Start();
        }

        private void Heartbeat_Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {

                string msg = "{\"a\": \"h\", \"v\": [], \"m\": \"\"}";//heartbeat

                if (IsConnected)
                    _ws.Send(msg);

            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
