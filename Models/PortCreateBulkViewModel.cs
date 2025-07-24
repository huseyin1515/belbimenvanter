using System.Collections.Generic;
namespace BelbimEnv.Models
{
    public class PortCreateBulkViewModel
    {
        public int ServerId { get; set; }
        public string? ServerName { get; set; }
        public List<PortDetay> Portlar { get; set; } = new List<PortDetay>();
    }
}