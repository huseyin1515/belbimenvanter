// Models/RegisterViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace BelbimEnv.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        // === YENİ EKLENEN ALANLAR ===
        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [EmailAddress]
        [Display(Name = "Email Adresi")]
        public string Email { get; set; }
        // ============================

        [Required(ErrorMessage = "Parola zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Parola en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Parolayı Onayla")]
        [Compare("Password", ErrorMessage = "Parolalar eşleşmiyor.")]
        public string ConfirmPassword { get; set; }
    }
}