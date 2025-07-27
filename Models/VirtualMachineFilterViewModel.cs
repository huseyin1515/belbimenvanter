// Models/VirtualMachineFilterViewModel.cs
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BelbimEnv.Models
{
    public class FilterOption
    {
        public string Text { get; set; }
        public bool IsSelected { get; set; }
    }

    public class VirtualMachineFilterViewModel
    {
        // Filtrelenen VM listesi
        public IEnumerable<VirtualMachine> VirtualMachines { get; set; }

        // Filtreleme seçenekleri
        public List<string> SelectedStatuses { get; set; } = new List<string>();
        public List<string> SelectedNetworks { get; set; } = new List<string>();
        public List<string> SelectedHosts { get; set; } = new List<string>();
        public List<string> SelectedClusters { get; set; } = new List<string>();

        // Checkbox listeleri için
        public List<SelectListItem> AllStatuses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllNetworks { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllHosts { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllClusters { get; set; } = new List<SelectListItem>();
    }
}