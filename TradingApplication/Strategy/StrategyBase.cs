using AliceBlueWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trading_App.Processor;

namespace TradingApplication.Strategy
{
    public delegate void MessasgeHandler(string value);
    public abstract class StrategyBase
    {
        public event MessasgeHandler OnMessage;
        public event OnErrorHandler OnError;
        

        public string CurrentBankNfity { get; set; }

        public abstract void Subscribe(APIProcessor apiProcessor);

        public abstract void Unsubscribe();

        public StrategyBase()
        {
            OnMessage += StrategyBase_OnMessage;
        }

        protected virtual void StrategyBase_OnMessage(string value)
        {
            OnError?.Invoke(value);
        }
    }
}
