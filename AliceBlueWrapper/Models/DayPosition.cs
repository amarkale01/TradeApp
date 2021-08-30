using System;
using System.Collections.Generic;
using System.Text;

namespace AliceBlueWrapper.Models
{
    public class DayPosition
    {
        public DayPosition()
        {
            data = new DayPositionData();
        }
        public string status { get; set; }
        public string message { get; set; }
        public DayPositionData data { get; set; }
    }

    public class DayPositionData
    {
        public List<Position> positions { get; set; }
        public DayPositionData()
        {
            positions = new List<Position>();
        }
    }
    public class Position
    {   
        public string unrealised_pnl { get; set; }
        public string trading_symbol { get; set; }
        public string total_sell_quantity { get; set; }
        public string total_buy_quantity { get; set; }
        public string strike_price { get; set; }
        public string sell_quantity { get; set; }
        public string sell_amount { get; set; }
        public string realised_pnl { get; set; }
        public string product { get; set; }
        public string oms_order_id { get; set; }
        public int net_quantity { get; set; } 
        public string net_amount { get; set; }
        public string multiplier { get; set; }
        public string m2m { get; set; }
        public string ltp { get; set; }
        public string instrument_token { get; set; }
        public string fill_id { get; set; }
        public string exchange { get; set; }
        public string enabled { get; set; } 
        public string close_price { get; set; }
        public string client_id { get; set; }
        public string cf_sell_quantity { get; set; }
        public string cf_buy_quantity { get; set; }
        public string cf_average_sell_price { get; set; }
        public string cf_average_buy_price { get; set; }
        public string buy_quantity { get; set; }
        public string buy_amount { get; set; }
        public string bep { get; set; }
        public string average_sell_price { get; set; }
        public string average_buy_price { get; set; }        

    }
}
