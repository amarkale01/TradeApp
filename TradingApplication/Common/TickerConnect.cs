using AliceBlueWrapper;
using AliceBlueWrapper.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Trading_App.Processor;

namespace Trading_App.Common
{
    public delegate void TickerHandler(string value);
    public delegate void TickerTickHandler(Tick value);
    public class TickerConnect : IDisposable
    {
        public event TickerHandler OnTickerChanged;
        public event TickerTickHandler OnTickerTickChanged;
        public event OnErrorHandler OnError;

        public uint BANKNIFTY_INSTRUMENT_TOKEN = 26009;
        public uint NIFTY_INSTRUMENT_TOKEN = 26000;

        Timer timer;
        APIProcessor apiProcessor;
        TickerTape tickerTape = null;


        public TickerConnect(APIProcessor apiProcessor)
        {
            this.apiProcessor = apiProcessor;


            timer = new Timer
            {
                Interval = 600
            };
        }

        public void TimerStart()
        {

            //timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            //timer.Start();
        }

        //private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    try
        //    {

        //        string tickerValue = tickerTape.bankNiftyValue;

        //        if (!string.IsNullOrEmpty(tickerValue))
        //        {
        //            OnTickerChanged?.Invoke(tickerValue);
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        OnError?.Invoke(ex.Message);
        //    }
        //}

        public void SubscribeTicker(string token,List<Tick> ticks)
        {
            tickerTape = new TickerTape(token, ticks);
            tickerTape.OnError += tickerTapeError;
            tickerTape.OnTickerTick += TickerTape_OnTickerTick;
            //TimerStart();

        }

        public void ReSubscribeTicker(string token, List<Tick> ticks)
        {
            tickerTape = new TickerTape(token, ticks);
            tickerTape.OnError += tickerTapeError;
            tickerTape.OnTickerTick += TickerTape_OnTickerTick;
            //TimerStart();

        }

        private void TickerTape_OnTickerTick(Tick tick)
        {
            try
            {
                OnTickerTickChanged?.Invoke(tick);
            }
            catch (Exception ex)
            {
                
            }
        } 

        private void tickerTapeError(string errorMessage)
        {
            OnError?.Invoke(errorMessage);
        }

        public void Dispose()
        {
            if (tickerTape != null)
                tickerTape.Dispose();
        }
    }
}
