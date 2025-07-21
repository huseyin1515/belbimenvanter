using System.ComponentModel.DataAnnotations;

namespace BelbimEnv.Models
{
    public enum PortTipiEnum
    {
        [Display(Name = "SAN")]
        SAN = 1,

        [Display(Name = "Bakır")]
        Bakir = 2,

        [Display(Name = "Fiber Channel (FC)")]
        FC = 3,

        [Display(Name = "Virtual Fiber Channel (vFC)")]
        VirtualFC = 4,

        [Display(Name = "Virtual")]
        Virtual = 5,

        [Display(Name = "Virtual Bakır")]
        VirtualBakir = 6
    }
}