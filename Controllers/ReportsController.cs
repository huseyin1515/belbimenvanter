// Controllers/ReportsController.cs
using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var allPorts = await _context.PortDetaylari.Include(p => p.Server).AsNoTracking().ToListAsync();
            var allServers = await _context.Servers.Include(s => s.PortDetaylari).AsNoTracking().ToListAsync();

            int totalUp = allPorts.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "up");
            int totalPorts = allPorts.Count;

            var viewModel = new OverallReportViewModel
            {
                TotalServers = allServers.Count,
                TotalPorts = totalPorts,
                TotalUpPorts = totalUp,
                TotalDownPorts = allPorts.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "down"),
                OverallUpPercentage = totalPorts > 0 ? (double)totalUp / totalPorts * 100 : 0,

                PortsByType = allPorts.GroupBy(p => p.PortTipi.ToString())
                                      .ToDictionary(g => g.Key, g => g.Count()),

                PortsByLocation = allPorts.Where(p => p.Server != null && !string.IsNullOrEmpty(p.Server.Location))
                                          .GroupBy(p => p.Server.Location)
                                          .ToDictionary(g => g.Key, g => g.Count()),

                // YENİ HESAPLAMALAR
                TopServersByPortCount = allServers.OrderByDescending(s => s.PortDetaylari.Count)
                                                  .Take(5) // En çok porta sahip ilk 5 sunucu
                                                  .ToDictionary(s => s.HostDns, s => s.PortDetaylari.Count),

                PortStatusDistribution = new Dictionary<string, int>
                {
                    { "Up", totalUp },
                    { "Down", allPorts.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "down") },
                    { "Bilinmiyor", allPorts.Count(p => string.IsNullOrEmpty(p.LinkStatus) || (p.LinkStatus.ToLower() != "up" && p.LinkStatus.ToLower() != "down")) }
                },

                ServerReports = allServers.Select(s => new ServerReportViewModel
                {
                    ServerId = s.Id,
                    HostDns = s.HostDns,
                    TotalPorts = s.PortDetaylari.Count,
                    UpPorts = s.PortDetaylari.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "up"),
                    DownPorts = s.PortDetaylari.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "down"),
                    UpPercentage = s.PortDetaylari.Any() ? (double)s.PortDetaylari.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "up") / s.PortDetaylari.Count * 100 : 0
                }).OrderByDescending(r => r.TotalPorts).ToList()
            };

            return View(viewModel);
        }
    }
}