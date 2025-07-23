using BelbimEnv.Data;
using BelbimEnv.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    [Authorize]
    public class PortDetaylariController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PortDetaylariController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUser")] // Sadece SuperUser erişebilir
        public async Task<IActionResult> ImportExcel(IFormFile file, string importOption)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen bir Excel (.xlsx) dosyası seçin.";
                return RedirectToAction(nameof(ListAll));
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var portsToImport = new List<PortDetay>();
            var notFoundServiceTags = new List<string>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true } });
                        DataTable dataTable = result.Tables[0];

                        var allServers = await _context.Servers.ToListAsync();
                        var serversDict = allServers
                            .Where(s => !string.IsNullOrEmpty(s.ServiceTag))
                            .GroupBy(s => s.ServiceTag.Trim(), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        foreach (DataRow row in dataTable.Rows)
                        {
                            string serviceTag = row.Table.Columns.Contains("Device Service Tag") ? row["Device Service Tag"]?.ToString()?.Trim() :
                                               (row.Table.Columns.Contains("Device Service Ta*") ? row["Device Service Ta*"]?.ToString()?.Trim() : null);
                            if (string.IsNullOrEmpty(serviceTag)) continue;

                            if (serversDict.TryGetValue(serviceTag, out Server server))
                            {
                                PortTipiEnum portTipi = default;
                                string turuStr = row.Table.Columns.Contains("Türü") ? row["Türü"]?.ToString() : "";
                                if (!string.IsNullOrEmpty(turuStr))
                                {
                                    Enum.TryParse<PortTipiEnum>(turuStr.Replace(" ", ""), true, out portTipi);
                                }
                                if (portTipi == default(PortTipiEnum)) continue;

                                var port = new PortDetay
                                {
                                    ServerId = server.Id,
                                    PortTipi = portTipi,
                                    LinkStatus = row.Table.Columns.Contains("Link Status") ? row["Link Status"]?.ToString() : null,
                                    LinkSpeed = row.Table.Columns.Contains("Link Speed") ? row["Link Speed"]?.ToString() : (row.Table.Columns.Contains("Link Spee*") ? row["Link Spee*"]?.ToString() : null),
                                    PortId = row.Table.Columns.Contains("Port ID") ? row["Port ID"]?.ToString() : (row.Table.Columns.Contains("Port-ID") ? row["Port-ID"]?.ToString() : null),
                                    NicId = row.Table.Columns.Contains("NIC ID") ? row["NIC ID"]?.ToString() : null,
                                    FiberMAC = row.Table.Columns.Contains("Fiber MAC") ? row["Fiber MAC"]?.ToString() : null,
                                    BakirMAC = row.Table.Columns.Contains("Bakır MAC") ? row["Bakır MAC"]?.ToString() : null,
                                    Wwpn = row.Table.Columns.Contains("WWPN") ? row["WWPN"]?.ToString() : null,
                                    SwName = row.Table.Columns.Contains("SW NAME") ? row["SW NAME"]?.ToString() : null,
                                    SwPort = row.Table.Columns.Contains("SW PORT") ? row["SW PORT"]?.ToString() : (row.Table.Columns.Contains("SW POR*") ? row["SW POR*"]?.ToString() : null),
                                    SwdeUcMi = row.Table.Columns.Contains("Swde Up Mı?") ? row["Swde Up Mı?"]?.ToString() : (row.Table.Columns.Contains("Swde Uç mı?") ? row["Swde Uç mı?"]?.ToString() : null),
                                    FcUcPortSayisi = row.Table.Columns.Contains("FC_Up Port Sayısı") && int.TryParse(row["FC_Up Port Sayısı"]?.ToString(), out int fcCount) ? fcCount : null,
                                    BakirUplinkPort = row.Table.Columns.Contains("BakırUpPort") ? row["BakırUpPort"]?.ToString() : (row.Table.Columns.Contains("BakırUplinkPor*") ? row["BakırUplinkPor*"]?.ToString() : null),
                                    UcPort = row.Table.Columns.Contains("Up Port") ? row["Up Port"]?.ToString() : null
                                };

                                port.Aciklama = GeneratePortDescription(port, server);
                                portsToImport.Add(port);
                            }
                            else if (!notFoundServiceTags.Contains(serviceTag))
                            {
                                notFoundServiceTags.Add(serviceTag);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Excel dosyası okunurken hata: {ex.Message}.";
                return RedirectToAction(nameof(ListAll));
            }

            try
            {
                if (importOption == "replace") { _context.PortDetaylari.RemoveRange(_context.PortDetaylari); }
                await _context.PortDetaylari.AddRangeAsync(portsToImport);
                await _context.SaveChangesAsync();

                string successMessage = $"{portsToImport.Count} port başarıyla yüklendi.";
                if (notFoundServiceTags.Any()) { TempData["WarningMessage"] = $"{successMessage} Ancak şu Service Tag'lere sahip sunucular bulunamadığı için bu portlar atlandı: {string.Join(", ", notFoundServiceTags)}"; }
                else { TempData["SuccessMessage"] = successMessage; }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Veritabanına kayıt sırasında hata: {ex.InnerException?.Message ?? ex.Message}";
            }
            return RedirectToAction(nameof(ListAll));
        }

        private string GeneratePortDescription(PortDetay port, Server server)
        {
            string deviceName = server.HostDns ?? "UnknownServer";
            string macAddress = "";
            if (port.PortTipi == PortTipiEnum.Bakir || port.PortTipi == PortTipiEnum.VirtualBakir) macAddress = port.BakirMAC;
            else if (port.PortTipi == PortTipiEnum.FC || port.PortTipi == PortTipiEnum.VirtualFC) macAddress = port.FiberMAC;
            else if (port.PortTipi == PortTipiEnum.SAN) macAddress = port.Wwpn;
            string lastFourDigits = "XXXX";
            if (!string.IsNullOrEmpty(macAddress))
            {
                string cleanMac = new string(macAddress.Where(char.IsLetterOrDigit).ToArray());
                if (cleanMac.Length >= 4) { lastFourDigits = new string(cleanMac.TakeLast(4).ToArray()).ToUpper(); }
            }
            string typeInitial = port.PortTipi.ToString().ToUpper().FirstOrDefault().ToString();
            string countInfo = "0";
            if ((port.PortTipi == PortTipiEnum.Bakir || port.PortTipi == PortTipiEnum.VirtualBakir) && !string.IsNullOrEmpty(port.BakirUplinkPort)) { countInfo = port.BakirUplinkPort; }
            else if ((port.PortTipi == PortTipiEnum.FC || port.PortTipi == PortTipiEnum.VirtualFC) && port.FcUcPortSayisi.HasValue) { countInfo = port.FcUcPortSayisi.Value.ToString(); }
            return $"{deviceName}_{lastFourDigits}_{typeInitial}_{countInfo}";
        }
        [Authorize(Roles = "SuperUser,User")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var portDetay = await _context.PortDetaylari.Include(p => p.Server).AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (portDetay == null) return NotFound();
            return View(portDetay);
        }
        [Authorize(Roles = "SuperUser,User")]
        public async Task<IActionResult> ListAll(string sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["ServerSortParm"] = String.IsNullOrEmpty(sortOrder) ? "server_desc" : "";
            ViewData["LocationSortParm"] = sortOrder == "location" ? "location_desc" : "location";
            ViewData["ModelSortParm"] = sortOrder == "model" ? "model_desc" : "model";
            ViewData["ServiceTagSortParm"] = sortOrder == "servicetag" ? "servicetag_desc" : "servicetag";
            ViewData["PortTypeSortParm"] = sortOrder == "porttype" ? "porttype_desc" : "porttype";

            var ports = from p in _context.PortDetaylari.Include(p => p.Server)
                        select p;

            switch (sortOrder)
            {
                case "server_desc": ports = ports.OrderByDescending(p => p.Server.HostDns); break;
                case "location": ports = ports.OrderBy(p => p.Server.Location); break;
                case "location_desc": ports = ports.OrderByDescending(p => p.Server.Location); break;
                case "model": ports = ports.OrderBy(p => p.Server.Model); break;
                case "model_desc": ports = ports.OrderByDescending(p => p.Server.Model); break;
                case "servicetag": ports = ports.OrderBy(p => p.Server.ServiceTag); break;
                case "servicetag_desc": ports = ports.OrderByDescending(p => p.Server.ServiceTag); break;
                case "porttype": ports = ports.OrderBy(p => p.PortTipi); break;
                case "porttype_desc": ports = ports.OrderByDescending(p => p.PortTipi); break;
                default: ports = ports.OrderBy(p => p.Server.HostDns); break;
            }

            return View(await ports.AsNoTracking().ToListAsync());
        }
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Manage(int? id)
        {
            if (id == null) return NotFound("Sunucu ID'si belirtilmedi.");
            var server = await _context.Servers.Include(s => s.PortDetaylari).AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (server == null) return NotFound($"ID'si {id} olan sunucu bulunamadı.");
            var viewModel = new PortDetayViewModel
            {
                ServerId = server.Id,
                HostDns = server.HostDns,
                ServiceTag = server.ServiceTag,
                Model = server.Model,
                Location = server.Location,
                Portlar = server.PortDetaylari.OrderBy(p => p.Id).ToList(),
                YeniPortFormu = new PortDetay { ServerId = server.Id }
            };
            return View(viewModel);
        }
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Create(int serverId)
        {
            var server = await _context.Servers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null) return NotFound("Port eklenecek sunucu bulunamadı.");
            var viewModel = new PortCreateBulkViewModel { ServerId = serverId, ServerName = server.HostDns };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Create(PortCreateBulkViewModel viewModel)
        {
            var server = await _context.Servers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == viewModel.ServerId);
            if (server == null) { TempData["ErrorMessage"] = "Portların ekleneceği sunucu bulunamadı."; return RedirectToAction("Index", "Servers"); }
            int addedCount = 0;
            if (viewModel.Portlar != null)
            {
                var validPorts = viewModel.Portlar.Where(p => p.PortTipi != default(PortTipiEnum));
                foreach (var port in validPorts)
                {
                    port.ServerId = viewModel.ServerId;
                    port.Id = 0;
                    port.Aciklama = GeneratePortDescription(port, server);
                    _context.PortDetaylari.Add(port);
                    addedCount++;
                }
            }
            if (addedCount > 0) { await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"{addedCount} adet port başarıyla eklendi."; }
            else { TempData["ErrorMessage"] = "Kaydedilecek geçerli bir port girilmedi."; }
            return RedirectToAction(nameof(Manage), new { id = viewModel.ServerId });
        }
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var portDetay = await _context.PortDetaylari.FindAsync(id);
            if (portDetay == null) return NotFound();
            return View(portDetay);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Edit(int id, PortDetay portDetayFromForm)
        {
            if (id != portDetayFromForm.Id) return NotFound();
            var server = await _context.Servers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == portDetayFromForm.ServerId);
            if (server == null) return NotFound("Portun ait olduğu sunucu bulunamadı.");
            if (ModelState.IsValid)
            {
                try
                {
                    portDetayFromForm.Aciklama = GeneratePortDescription(portDetayFromForm, server);
                    _context.Update(portDetayFromForm);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Port başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.PortDetaylari.Any(e => e.Id == portDetayFromForm.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Details", "Servers", new { id = portDetayFromForm.ServerId });
            }
            return View(portDetayFromForm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Delete(int id)
        {
            var portDetay = await _context.PortDetaylari.FindAsync(id);
            if (portDetay == null) { TempData["ErrorMessage"] = "Silinecek port bulunamadı."; return RedirectToAction("Index", "Servers"); }
            int serverId = portDetay.ServerId;
            _context.PortDetaylari.Remove(portDetay);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Port başarıyla silindi.";
            return RedirectToAction("Details", "Servers", new { id = serverId });
        }
    }
}