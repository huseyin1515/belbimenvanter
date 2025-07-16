using System.Collections.Generic;

namespace BelbimEnv.Models
{
    public class PortDetayViewModel
    {
        public int ServerId { get; set; }
        public string? DeviceName { get; set; }
        public List<PortDetay> Portlar { get; set; } = new List<PortDetay>();
    }
}