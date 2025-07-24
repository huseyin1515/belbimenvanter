namespace BelbimEnv.Models
{
    public class ServerReportViewModel
    {
        public int ServerId { get; set; }
        public string? HostDns { get; set; }
        public int TotalPorts { get; set; }
        public int UpPorts { get; set; }
        public int DownPorts { get; set; }
        public double UpPercentage { get; set; }
    }
}