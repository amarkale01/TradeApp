using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trading_App.Processor;

namespace TradingApplication.Strategy
{
    public class BankNiftyOptionSpreadStrategy : StrategyBase, IDisposable
    {
        APIProcessor apiProcessor;

        public async override void Subscribe(APIProcessor apiProcessor)
        {
            this.apiProcessor = apiProcessor;
            
        }

        protected override void StrategyBase_OnMessage(string value)
        {
            base.StrategyBase_OnMessage(value);
        }

        public override void Unsubscribe()
        {
            
        }

        public async void Run(APIProcessor apiProcessor)
        {

            int mainStrike = apiProcessor.Strike;
           
            if(apiProcessor.IsCEChecked)
                apiProcessor.Strike = mainStrike + 500;
            else
                apiProcessor.Strike = mainStrike - 500;
            
            apiProcessor.TransactionOrderType = "BUY";

            await apiProcessor.PlaceEntryOrder("BANKNIFTY");
            StrategyBase_OnMessage("BUY " + apiProcessor.Strike + " " + (apiProcessor.IsCEChecked == true ? "CE":"PE"));
            System.Threading.Thread.Sleep(5000);
            
            apiProcessor.Strike = mainStrike;
            apiProcessor.TransactionOrderType = "SELL";

            await apiProcessor.PlaceEntryOrder("BANKNIFTY");

            StrategyBase_OnMessage("SELL " + apiProcessor.Strike + " " + (apiProcessor.IsCEChecked == true ? "CE" : "PE"));
        }

        public void Dispose()
        {
            
        }
    }
}
