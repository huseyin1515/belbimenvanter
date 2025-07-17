// Models/PortDetay.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // <<<--- BU USING SATIRINI EKLEYİN

namespace BelbimEnv.Models
{
    public class PortDetay
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ait Olduğu Sunucu")]
        public int ServerId { get; set; }

        [ForeignKey("ServerId")]
        [ValidateNever] // <<<--- SORUNU ÇÖZEN ATTRIBUTE BUDUR.
        public virtual Server Server { get; set; } = null!;

        [Required(ErrorMessage = "Port tipi seçimi zorunludur.")]
        [Display(Name = "Port Tipi")]
        public PortTipiEnum PortTipi { get; set; }

        [Display(Name = "Link Durumu")]
        public string? LinkStatus { get; set; }

        [Display(Name = "Link Hızı")]
        public string? LinkSpeed { get; set; }

        [Display(Name = "Port ID")]
        public string? PortId { get; set; }

        [Display(Name = "NIC ID")]
        public string? NicId { get; set; }

        [Display(Name = "Fiber MAC")]
        public string? FiberMAC { get; set; }

        [Display(Name = "Bakır MAC")]
        public string? BakirMAC { get; set; }

        [Display(Name = "WWPN")]
        public string? Wwpn { get; set; }

        [Display(Name = "Switch Adı")]
        public string? SwName { get; set; }

        [Display(Name = "Switch Port")]
        public string? SwPort { get; set; }

        [Display(Name = "Switch'de Uç mü?")]
        public string? SwdeUcMi { get; set; }

        [Display(Name = "Uç Port")]
        public string? UcPort { get; set; }

        [Display(Name = "Bakır Uplink Port")]
        public string? BakirUplinkPort { get; set; }

        [Display(Name = "FC Uç Port Sayısı")]
        public int? FcUcPortSayisi { get; set; }

        [Display(Name = "Açıklama (Otomatik)")]
        public string? Aciklama { get; set; }
    }
}