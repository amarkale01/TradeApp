using System;
using System.Collections.Generic;
using System.Text;

namespace AliceBlueWrapper.Models
{
    public class OrderResponse
    {
        public OrderResponse()
        {
            data = new OrderResponseData();
        }
        public string status { get; set; }
        public string message { get; set; }
        public OrderResponseData data { get; set; }
    }

    public class OrderResponseData
    {
        public string oms_order_id { get; set; }
    }
}
