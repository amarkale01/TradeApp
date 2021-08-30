using System;
using System.Collections.Generic;
using System.Text;

namespace Trading_App.Model
{
    public struct OrderType
    {
        public const string Market = "MARKET";
        public const string Limit = "LIMIT";
        public const string StopLossLimit = "SL";
        public const string StopLossMarket = "SL-M";
    }

    public struct TransactionType
    {
        public const string Buy = "BUY";
        public const string Sell = "SELL";
    }
    
    public struct ProductType
    {
        // if(instrument.exchange == 'NFO') or(instrument.exchange == 'MCX') or(instrument.exchange == 'CDS') 
        // then product type NRML
        public const string Delivery = "NRML";
       
        public const string Intraday = "MIS";
        public const string CoverOrder = "CO";
        public const string BracketOrder = "BO";
    }
        
}
