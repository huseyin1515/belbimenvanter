// Models/ExportViewModel.cs
using System.Collections.Generic;

namespace BelbimEnv.Models
{
    public class ExportColumn
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ExportViewModel
    {
        public List<ExportColumn> Columns { get; set; } = new List<ExportColumn>();
    }
}