// Models/User.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace BelbimEnv.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // "SuperUser", "User", "Bekleyen"

        // === YENİ EKLENEN ALANLAR ===
        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir email adresi girin.")]
        public string Email { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; }
    }
}