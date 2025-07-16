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

        [Display(Name = "Adres (MAC/WWPN)")]
        public string? Adres { get; set; }

        public string? LinkStatus { get; set; }
        public string? LinkSpeed { get; set; }

        public int ServerId { get; set; }
        [ForeignKey("ServerId")]
        public virtual Server Server { get; set; }
    }
}