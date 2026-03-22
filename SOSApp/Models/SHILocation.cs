using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOSApp.Models
{
    public class SHILocation
    {
        public string DeviceName { get; set; }
        public decimal? SHILatitude { get; set; }
        public decimal? SHILongitude { get; set; }
    }
}