using System;
using System.Collections.Generic;
using System.Text;

namespace AliceBlueWrapper.Models
{
    public class Instrument
    {        
        public string symbol { get; set; }
        public string lotSize { get; set; }
        public string expiry { get; set; }
        public int exchange_code { get; set; }
        public string exchange { get; set; }
        public string company { get; set; }
        public string code { get; set; }
    }
}
