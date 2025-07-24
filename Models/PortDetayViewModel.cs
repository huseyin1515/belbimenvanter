using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace BelbimEnv.Models
{
    public class PortDetayViewModel
    {
        public int ServerId { get; set; }
        [Display(Name = "Host DNS")]
        public string? HostDns { get; set; }
        [Display(Name = "Service Tag")]
        public string? ServiceTag { get; set; }
        public string? Model { get; set; }
        public string? Location { get; set; }
        public List<PortDetay> Portlar { get; set; } = new List<PortDetay>();
        public PortDetay? YeniPortFormu { get; set; }
    }
}