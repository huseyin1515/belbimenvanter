using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Verileri tek seferde çek
            var allPorts = await _context.PortDetaylari.Include(p => p.Server).AsNoTracking().ToListAsync();
            var allServers = await _context.Servers.Include(s => s.PortDetaylari).AsNoTracking().ToListAsync();
            var allVirtualMachines = await _context.VirtualMachines.AsNoTracking().ToListAsync(); // YENİ

            int totalUp = allPorts.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "up");
            int totalPorts = allPorts.Count;

            var viewModel = new OverallReportViewModel
            {
                // Fiziksel Sunucu ve Port İstatistikleri (Mevcut)
                TotalServers = allServers.Count,
                TotalPorts = totalPorts,
                TotalUpPorts = totalUp,
                TotalDownPorts = allPorts.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "down"),
                OverallUpPercentage = totalPorts > 0 ? (double)totalUp / totalPorts * 100 : 0,

                PortsByType = allPorts
                    .GroupBy(p => p.PortTipi.ToString())
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.Count()),

                PortsByLocation = allPorts
                    .Where(p => p.Server != null && !string.IsNullOrEmpty(p.Server.Location))
                    .GroupBy(p => p.Server.Location!)
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.Count()),

                TopServersByPortCount = allServers
                    .Where(s => !string.IsNullOrEmpty(s.HostDns))
                    .Select(s => new { HostDns = s.HostDns, PortCount = s.PortDetaylari.Count })
                    .OrderByDescending(x => x.PortCount)
                    .Take(5)
                    .ToDictionary(x => x.HostDns!, x => x.PortCount),

                PortStatusDistribution = new Dictionary<string, int>
                {
                    { "Up", totalUp },
                    { "Down", allPorts.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "down") },
                    { "Bilinmiyor", allPorts.Count(p => string.IsNullOrEmpty(p.LinkStatus) || (p.LinkStatus.ToLower() != "up" && p.LinkStatus.ToLower() != "down")) }
                },

                ServerReports = allServers
                    .Select(s => new ServerReportViewModel
                    {
                        ServerId = s.Id,
                        HostDns = s.HostDns,
                        TotalPorts = s.PortDetaylari.Count,
                        UpPorts = s.PortDetaylari.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "up"),
                        DownPorts = s.PortDetaylari.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "down"),
                        UpPercentage = s.PortDetaylari.Any() ? (double)s.PortDetaylari.Count(p => p.LinkStatus != null && p.LinkStatus.ToLower() == "up") / s.PortDetaylari.Count * 100 : 0
                    })
                    .OrderByDescending(r => r.UpPercentage)
                    .ThenByDescending(r => r.TotalPorts)
                    .ToList(),

                // === YENİ EKLENEN İSTATİSTİKLERİN HESAPLANMASI ===
                TotalVirtualMachines = allVirtualMachines.Count,
                TotalActiveVMs = allVirtualMachines.Count(vm => vm.Status != null && vm.Status.ToLower() == "poweredon"),

                OsDistribution = allServers
                    .Where(s => !string.IsNullOrEmpty(s.OS))
                    .GroupBy(s => s.OS!)
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.Count()),

                TopClustersByVmCount = allVirtualMachines
                    .Where(vm => !string.IsNullOrEmpty(vm.Cluster))
                    .GroupBy(vm => vm.Cluster!)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return View(viewModel);
        }
    }
}