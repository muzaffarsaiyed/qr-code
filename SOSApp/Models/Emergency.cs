using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOSApp.Models
{
    public class Emergency
    {
        public int ID { get; set; }
        public int SrNo { get; set; }
        public string CoverNo { get; set; }
        public string LastName { get; set; }
        public string PilgrimName { get; set; }
        public string Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Remarks { get; set; }
        public string Image1Path { get; set; }
        public string Image2Path { get; set; }
        public string VideoPath { get; set; }
        public string AudioPath { get; set; }
        public bool IsResolved { get; set; }
        public decimal? SHILatitude { get; set; }
        public decimal? SHILongitude { get; set; }
        public string DeviceName { get; set; }
    }
}