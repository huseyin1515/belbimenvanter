using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    public class VisualizationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int RACK_SIZE_U = 42;

        public VisualizationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var allLocations = await _context.Servers
                .Where(s => !string.IsNullOrEmpty(s.Location))
                .Select(s => s.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            return View(allLocations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindServer(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                TempData["ErrorMessage"] = "Lütfen bir arama terimi girin.";
                return RedirectToAction(nameof(Index));
            }

            searchTerm = searchTerm.Trim();

            var server = await _context.Servers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.HostDns.Contains(searchTerm) || s.ServiceTag.Contains(searchTerm));

            if (server == null)
            {
                TempData["ErrorMessage"] = $"'{searchTerm}' ile eşleşen bir sunucu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(server.Location) || string.IsNullOrEmpty(server.Kabin) || string.IsNullOrEmpty(server.KabinU))
            {
                TempData["WarningMessage"] = $"'{server.HostDns}' sunucusu bulundu, ancak kabin konum bilgileri eksik olduğu için görselleştirilemiyor. Sunucunun kendi detay sayfası açıldı.";
                return RedirectToAction("Details", "Servers", new { id = server.Id });
            }

            return RedirectToAction(nameof(RackView), new { location = server.Location, highlight = server.Id });
        }

        public async Task<IActionResult> RackView(string location)
        {
            if (string.IsNullOrEmpty(location)) { return RedirectToAction(nameof(Index)); }

            var allServers = await _context.Servers.AsNoTracking().ToListAsync();
            var allLocations = allServers.Where(s => !string.IsNullOrEmpty(s.Location)).Select(s => s.Location).Distinct().OrderBy(l => l).ToList();
            var viewModel = new RackVisualizationViewModel
            {
                AllLocations = allLocations,
                SelectedLocation = location
            };

            var serversInLocation = allServers.Where(s => s.Location == location && !string.IsNullOrEmpty(s.Kabin) && !string.IsNullOrEmpty(s.KabinU)).ToList();
            var cabinetsInLocation = serversInLocation.Select(s => s.Kabin).Distinct().OrderBy(c => c);

            foreach (var cabinetName in cabinetsInLocation)
            {
                var frontU_Map = new Dictionary<int, List<Server>>();
                var rearU_Map = new Dictionary<int, List<Server>>();
                var serversInCabinet = serversInLocation.Where(s => s.Kabin == cabinetName);

                foreach (var server in serversInCabinet)
                {
                    var (startU, endU) = ParseKabinU(server.KabinU);
                    if (startU == 0 || endU == 0) continue;
                    var targetMap = (server.RearFront == "R" || server.RearFront == "Rear") ? rearU_Map : frontU_Map;

                    for (int u = startU; u <= endU; u++)
                    {
                        if (!targetMap.ContainsKey(u)) { targetMap[u] = new List<Server>(); }
                        targetMap[u].Add(server);
                    }
                }

                var frontUnits = new List<RackUnitViewModel>();
                var rearUnits = new List<RackUnitViewModel>();
                for (int i = 1; i <= RACK_SIZE_U; i++)
                {
                    frontUnits.Add(new RackUnitViewModel { U_Number = i, OccupyingServers = frontU_Map.GetValueOrDefault(i, new List<Server>()) });
                    rearUnits.Add(new RackUnitViewModel { U_Number = i, OccupyingServers = rearU_Map.GetValueOrDefault(i, new List<Server>()) });
                }

                viewModel.Racks[cabinetName + " (Ön)"] = frontUnits;
                viewModel.Racks[cabinetName + " (Arka)"] = rearUnits;
            }
            return View(viewModel);
        }

        // YENİ EKLENEN METOT
        public async Task<IActionResult> RackSelector(string location, int serverIdToIgnore = 0)
        {
            if (string.IsNullOrEmpty(location))
            {
                var allLocations = await _context.Servers.Where(s => !string.IsNullOrEmpty(s.Location))
                                                        .Select(s => s.Location).Distinct().OrderBy(l => l).ToListAsync();
                return PartialView("_RackSelectorLocations", allLocations);
            }

            var allServers = await _context.Servers.AsNoTracking()
                                        .Where(s => s.Id != serverIdToIgnore)
                                        .ToListAsync();

            var viewModel = new RackVisualizationViewModel { SelectedLocation = location };
            var serversInLocation = allServers.Where(s => s.Location == location && !string.IsNullOrEmpty(s.Kabin) && !string.IsNullOrEmpty(s.KabinU)).ToList();

            // Lokasyondaki tüm kabinleri al (içleri boş bile olsa)
            var cabinetsInLocation = allServers.Where(s => s.Location == location && !string.IsNullOrEmpty(s.Kabin)).Select(s => s.Kabin).Distinct().OrderBy(c => c);

            foreach (var cabinetName in cabinetsInLocation)
            {
                var frontU_Map = new Dictionary<int, List<Server>>();
                var rearU_Map = new Dictionary<int, List<Server>>();
                var serversInCabinet = serversInLocation.Where(s => s.Kabin == cabinetName);

                foreach (var server in serversInCabinet)
                {
                    var (startU, endU) = ParseKabinU(server.KabinU);
                    if (startU == 0 || endU == 0) continue;
                    var targetMap = (server.RearFront == "R" || server.RearFront == "Rear") ? rearU_Map : frontU_Map;
                    for (int u = startU; u <= endU; u++)
                    {
                        if (!targetMap.ContainsKey(u)) { targetMap[u] = new List<Server>(); }
                        targetMap[u].Add(server);
                    }
                }

                var frontUnits = new List<RackUnitViewModel>();
                var rearUnits = new List<RackUnitViewModel>();
                for (int i = 1; i <= RACK_SIZE_U; i++)
                {
                    frontUnits.Add(new RackUnitViewModel { U_Number = i, OccupyingServers = frontU_Map.GetValueOrDefault(i, new List<Server>()) });
                    rearUnits.Add(new RackUnitViewModel { U_Number = i, OccupyingServers = rearU_Map.GetValueOrDefault(i, new List<Server>()) });
                }

                viewModel.Racks[cabinetName + " (Ön)"] = frontUnits;
                viewModel.Racks[cabinetName + " (Arka)"] = rearUnits;
            }
            return PartialView("_RackSelector", viewModel);
        }

        private (int, int) ParseKabinU(string kabinU)
        {
            if (string.IsNullOrWhiteSpace(kabinU)) return (0, 0);
            try
            {
                if (kabinU.Contains('-'))
                {
                    var parts = kabinU.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int start) && int.TryParse(parts[1].Trim(), out int end))
                    {
                        return (Math.Min(start, end), Math.Max(start, end));
                    }
                }
                else if (int.TryParse(kabinU.Trim(), out int singleU))
                {
                    return (singleU, singleU);
                }
            }
            catch { return (0, 0); }
            return (0, 0);
        }
    }
}