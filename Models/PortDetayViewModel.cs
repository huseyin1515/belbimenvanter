// Models/PortDetayViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BelbimEnv.Models
{
    // BU VIEWMODEL, PORT YÖNETİM SAYFASI İÇİN GEREKLİ TÜM BİLGİLERİ TAŞIR.
    public class PortDetayViewModel
    {
        // --- Ana Sunucu Bilgileri ---
        public int ServerId { get; set; }

        [Display(Name = "Host DNS")]
        public string? HostDns { get; set; }

        [Display(Name = "Service Tag")]
        public string? ServiceTag { get; set; }

        public string? Model { get; set; }
        public string? Location { get; set; }

        // --- Sunucuya Ait Portların Listesi ---
        public List<PortDetay> Portlar { get; set; } = new List<PortDetay>();

        // --- YENİ EKLENDİ: Yeni port ekleme formu için ---
        public PortDetay YeniPortFormu { get; set; }
    }
}