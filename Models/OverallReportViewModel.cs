using System.Collections.Generic;
namespace BelbimEnv.Models
{
    public class OverallReportViewModel
    {
        public int TotalServers { get; set; }
        public int TotalPorts { get; set; }
        public int TotalUpPorts { get; set; }
        public int TotalDownPorts { get; set; }
        public double OverallUpPercentage { get; set; }
        public Dictionary<string, int> PortsByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PortsByLocation { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TopServersByPortCount { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PortStatusDistribution { get; set; } = new Dictionary<string, int>();
        public List<ServerReportViewModel> ServerReports { get; set; } = new List<ServerReportViewModel>();
    }
}