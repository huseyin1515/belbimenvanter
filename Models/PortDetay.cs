using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BelbimEnv.Models
{
    public class PortDetay
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PortTipi { get; set; }

        [Display(Name = "Fiber MAC")]
        public string? FiberMAC { get; set; }

        [Display(Name = "Bakır MAC")]
        public string? BakirMAC { get; set; }

        [Display(Name = "WWPN")]
        public string? WWPN { get; set; }

        [Display(Name = "Link Durumu")]
        public string? LinkStatus { get; set; }

        [Display(Name = "Link Hızı")]
        public string? LinkSpeed { get; set; }

        [Display(Name = "Port ID")]
        public string? PortID { get; set; }

        [Display(Name = "NIC ID")]
        public string? NIC_ID { get; set; }

        [Display(Name = "SW NAME")]
        public string? SW_NAME { get; set; }

        [Display(Name = "SW PORT")]
        public string? SW_PORT { get; set; }

        // Foreign Key ilişkisi
        public int ServerId { get; set; }
        [ForeignKey("ServerId")]
        public virtual Server Server { get; set; }
    }
}