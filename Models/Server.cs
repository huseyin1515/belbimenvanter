using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BelbimEnv.Models
{
    public class Server
    {
        [Key]
        public int Id { get; set; }
        [Display(Name = "Host DNS")]
        public string? HostDns { get; set; }
        [Display(Name = "IP Adresi")]
        public string? IpAdress { get; set; }
        public string? Model { get; set; }
        [Display(Name = "Service Tag / Seri Numarası")]
        public string? ServiceTag { get; set; }
        [Display(Name = "vCenter Adresi")]
        public string? VcenterAdress { get; set; }
        public string? Cluster { get; set; }
        [Display(Name = "Lokasyon")]
        public string? Location { get; set; }
        [Display(Name = "İşletim Sistemi (O/S)")]
        public string? OS { get; set; }
        [Display(Name = "iLO/iDRAC IP")]
        public string? IloIdracIp { get; set; }
        [Display(Name = "Kabin")]
        public string? Kabin { get; set; }
        [Display(Name = "Yön (Rear/Front)")]
        public string? RearFront { get; set; }
        [Display(Name = "Kabin U")]
        public int? KabinU { get; set; }
        [Display(Name = "İsttelkom Etiket ID")]
        public string? IsttelkomEtiketId { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastUpdated { get; set; }

        public virtual ICollection<PortDetay> PortDetaylari { get; set; } = new List<PortDetay>();
    }
}