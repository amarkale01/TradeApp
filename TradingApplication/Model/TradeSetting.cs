using System;
using System.Collections.Generic;
using System.Text;

namespace Trading_App.Model
{
    public class TradeSetting
    {
        public string UserId { get; set; }
        public string ClientId { get; set; }
        public string Password { get; set; }
        public string ClientSecret { get; set; }
        public string Answer1 { get; set; }
        public string Answer2 { get; set; }
        public string Token { get; set; }
        public string TokenCreatedOn { get; set; }
        public string Quantity { get; set; }
        public string StopLossPercentage { get; set; }
        public string ExpiryWeek { get; set; }
        public string MTMProfit { get; set; }
        public string MTMLoss { get; set; }
        public bool IsStrangle { get; set; }
        public string OTMDiff { get; set; }
        public string Lots { get; set; }
        public string TradeAutoExecutionTime { get; set; }
    }
}
