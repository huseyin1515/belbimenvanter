using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // DisplayName için gerekli

namespace BelbimEnv.Models
{
    public class PortDetayViewModel
    {
        public int ServerId { get; set; }

        [Display(Name = "Host DNS")]
        public string? DeviceName { get; set; }

        [Display(Name = "Service Tag")]
        public string? ServiceTag { get; set; }

        [Display(Name = "Model")]
        public string? Model { get; set; }

        [Display(Name = "Lokasyon")]
        public string? Location { get; set; }

        public List<PortDetay> Portlar { get; set; } = new List<PortDetay>();
    }
}