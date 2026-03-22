using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOSApp.Models
{
    public class Medical
    {
        public int ID { get; set; }
        public int SrNo { get; set; }
        public string CoverNo { get; set; }
        public string LastName { get; set; }
        public string PilgrimName { get; set; }
        public string FatherName { get; set; }
        public string SpouseName { get; set; }
        public string PassportNo { get; set; }
        public string Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string Remarks { get; set; }
    }
}