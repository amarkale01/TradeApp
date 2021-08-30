using System;
using System.Collections.Generic;
using System.Text;

namespace AliceBlueWrapper.Models
{
    public class LoginDetail
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string userId { get; set; }
        public string password { get; set; }
        public string answer1 { get; set; }
        public string answer2 { get; set; }
    }
}
