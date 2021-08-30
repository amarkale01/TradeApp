using System;
using System.Collections.Generic;
using System.Text;

namespace AliceBlueWrapper.Models
{
    public  class Order
    {
        public string validity { get; set; }
        public string user_order_id { get; set; }
        public string trigger_price { get; set; }
        public string transaction_type { get; set; }
        public string trading_symbol { get; set; }
        public string remaining_quantity { get; set; }
        public string rejection_reason { get; set; }
        public string quantity { get; set; }
        public string product { get; set; }
        public string price { get; set; } = "0.0";
        public string order_type { get; set; }
        public string order_tag { get; set; }
        public string order_status { get; set; }
        public string order_entry_time { get; set; }
        public string oms_order_id { get; set; }
        public string nest_request_id { get; set; }
        public string lotsize { get; set; }
        public string login_id { get; set; }
        public string leg_order_indicator { get; set; }
        public string instrument_token { get; set; }
        public string filled_quantity { get; set; }
        public string exchange_time { get; set; }
        public string exchange_order_id { get; set; }
        public string exchange { get; set; } = "NFO";
        public string disclosed_quantity { get; set; }
        public string client_id { get; set; }
        public string average_price { get; set; }
        public string square_off_value { get; set; }
        public string stop_loss_value { get; set; }
    }
}
