using System;
using System.ComponentModel.DataAnnotations;

namespace BelbimEnv.Models
{
    public class VirtualMachine
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "SN")]
        public string? SN { get; set; }

        [Display(Name = "VM Adı")]
        public string? VmName { get; set; } // 'Ham' sütununa karşılık geliyor

        [Display(Name = "DNS")]
        public string? Dns { get; set; }

        [Display(Name = "VIP IP")]
        public string? VipIp { get; set; }

        [Display(Name = "Makine IP")]
        public string? MachineIp { get; set; } // 'Makine Ip' sütununa karşılık

        [Display(Name = "Port")]
        public string? Port { get; set; }

        [Display(Name = "Makine Adı")]
        public string? MachineName { get; set; }

        [Display(Name = "Durumu")]
        public string? Status { get; set; }

        [Display(Name = "Network")]
        public string? Network { get; set; }

        [Display(Name = "Cluster")]
        public string? Cluster { get; set; }

        [Display(Name = "Host")]
        public string? Host { get; set; }

        [Display(Name = "İşletim Sistemi")]
        public string? OS { get; set; }

        // YENİ EKLENEN POOL ALANI
        [Display(Name = "Pool")]
        public string? Pool { get; set; }

        public DateTime DateAdded { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}