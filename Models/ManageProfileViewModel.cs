// Models/ManageProfileViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace BelbimEnv.Models
{
    public class ManageProfileViewModel
    {
        public string Username { get; set; } // Değiştirilemez

        [Required]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Parola (İsteğe Bağlı)")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Parola en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Parolayı Onayla")]
        [Compare("NewPassword", ErrorMessage = "Parolalar eşleşmiyor.")]
        public string ConfirmPassword { get; set; }
    }
}