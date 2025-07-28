// Models/PortFilterViewModel.cs
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BelbimEnv.Models
{
    public class PortFilterViewModel
    {
        // Filtrelenen port listesi
        public IEnumerable<PortDetay> Ports { get; set; }

        // Filtreleme seçenekleri (formdan gelenler)
        public List<string> SelectedLocations { get; set; } = new List<string>();
        public List<string> SelectedPortTypes { get; set; } = new List<string>();
        public List<string> SelectedLinkStatuses { get; set; } = new List<string>();
        public List<string> SelectedSwNames { get; set; } = new List<string>();

        // Checkbox listeleri için (veritabanından gelenler)
        public List<SelectListItem> AllLocations { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllPortTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllLinkStatuses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllSwNames { get; set; } = new List<SelectListItem>();
    }
}