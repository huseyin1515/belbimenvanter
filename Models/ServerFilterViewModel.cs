// Models/ServerFilterViewModel.cs
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BelbimEnv.Models
{
    public class ServerFilterViewModel
    {
        // Filtrelenen sunucu listesi
        public IEnumerable<Server> Servers { get; set; }

        // Filtreleme seçenekleri (formdan gelenler)
        public List<string> SelectedLocations { get; set; } = new List<string>();
        public List<string> SelectedModels { get; set; } = new List<string>();
        public List<string> SelectedOS { get; set; } = new List<string>();
        public List<string> SelectedClusters { get; set; } = new List<string>();

        // Checkbox listeleri için (veritabanından gelenler)
        public List<SelectListItem> AllLocations { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllModels { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllOS { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllClusters { get; set; } = new List<SelectListItem>();
    }
}