using System;
using System.Collections.Generic;
using System.Text;

namespace AliceBlueWrapper.Models
{
    public class OrderHistory
    {
        public OrderHistory()
        {
            data = new Data();
        }
        public string status { get; set; }
        public string message { get; set; }
        public Data data { get; set; }      
    }

    public class Data
    {
        public Data()
        {
            pending_orders = new List<Order>();
            completed_orders = new List<Order>();
        }
        public List<Order> pending_orders { get; set; }
        public List<Order> completed_orders { get; set; }        
    }
}
