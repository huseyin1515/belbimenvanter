// Models/PortCreateBulkViewModel.cs
using System.Collections.Generic;

namespace BelbimEnv.Models
{
    public class PortCreateBulkViewModel
    {
        public int ServerId { get; set; }
        public string ServerName { get; set; }

        // Formdan gelen tüm port satırlarını bu liste karşılayacak.
        public List<PortDetay> Portlar { get; set; } = new List<PortDetay>();
    }
}