using AliceBlueWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trading_App.Processor;

namespace TradingApplication.Strategy
{
    public class StrategyManager
    {
        APIProcessor apiProcessor;
        public event OnErrorHandler LogMessage;

        public StrategyManager(APIProcessor apiProcessor)
        {
            this.apiProcessor = apiProcessor;
        }

        public StrategyBase CreateInstance(string strategyType)
        {
            StrategyBase strategyBase = null;

            try
            {
                switch (strategyType)
                {
                    case "BankNiftyTelegramStrategy":
                        LogMessage?.Invoke("Creating BankNiftyTelegramStrategy instance");
                        strategyBase = new BankNiftyTelegramStrategy();
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }

            return strategyBase;

        }
    }
}
