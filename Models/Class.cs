// Models/EditUserViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BelbimEnv.Models
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; }

        // Rol seçimi için dropdown listesi
        public List<SelectListItem> Roles { get; set; }
    }
}