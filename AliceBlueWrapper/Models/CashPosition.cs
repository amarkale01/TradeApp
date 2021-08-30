using System;
using System.Collections.Generic;
using System.Text;

namespace AliceBlueWrapper.Models
{
    public class CashDetail
    {
        public string status { get; set; }
        public string message { get; set; }
        public CashData data { get; set; }      
    }
    public class CashData
    {
        public CashData()
        {
            cash_positions = new List<CashPosition>();
        }
        public List<CashPosition> cash_positions { get; set; }
    }
    public class CashPosition
    {
        public CashPosition()
        {
            utilized = new Utilized();
            available = new Available();
        }
        public Utilized utilized { get; set; }
        public string segment { get; set; }
        public string net { get; set; }
        public string category { get; set; }
        public Available available { get; set; }
    }

    public class Utilized
    {
        public string var_margin { get; set; }
        public string unrealised_m2m { get; set; }
        public string span_margin { get; set; }
        public string realised_m2m { get; set; }
        public string premium_present { get; set; }
        public string pay_out { get; set; }
        public string multiplier { get; set; }
        public string exposure_margin { get; set; }
        public string elm { get; set; }
        public string debits { get; set; }       
    }
    public class Available
    {
        public string pay_in { get; set; }
        public string notionalCash { get; set; }
        public string direct_collateral_value { get; set; }
        public string credits { get; set; }
        public string collateral_value { get; set; }
        public string cashmarginavailable { get; set; }
        public string adhoc_margin { get; set; }   
        
    }
}
