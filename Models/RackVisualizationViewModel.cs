// Models/RackVisualizationViewModel.cs
using System.Collections.Generic;

namespace BelbimEnv.Models
{
    public class RackVisualizationViewModel
    {
        public string SelectedLocation { get; set; }
        public List<string> AllLocations { get; set; } = new List<string>();

        // Key: Kabin Adı (örn: "A101"), Value: O kabindeki U birimlerinin listesi
        public Dictionary<string, List<RackUnitViewModel>> Racks { get; set; } = new Dictionary<string, List<RackUnitViewModel>>();
    }
}