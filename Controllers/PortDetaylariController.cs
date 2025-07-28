using BelbimEnv.Data;
using BelbimEnv.Models;
using ClosedXML.Excel;
using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    [Authorize]
    public class PortDetaylariController : Controller
    {
        private readonly ApplicationDbContext _context;
        [HttpGet]
        public async Task<IActionResult> SearchServers(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<string>());
            }

            var serverNames = await _context.Servers
                .Where(s => s.HostDns.Contains(term))
                .Select(s => s.HostDns)
                .Take(10) // Çok fazla sonuç dönmemesi için limiti 10 ile sınırla
                .ToListAsync();

            return Json(serverNames);
        }
        public PortDetaylariController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GÜNCELLENMİŞ IMPORTEXCEL METODU (DETAYLI HATA TAKİBİ İLE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> ImportExcel(IFormFile file, string importOption)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen bir Excel (.xlsx) dosyası seçin.";
                return RedirectToAction(nameof(ListAll));
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var portsToImport = new List<PortDetay>();
            var skippedRows = new List<string>(); // Atlanan satırların sebeplerini tutacak liste

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
                        var serversDict = allServers.Where(s => !string.IsNullOrEmpty(s.ServiceTag)).GroupBy(s => s.ServiceTag.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        int rowNumber = 1; // Excel'deki satır numarasını takip etmek için (başlık satırından sonra)
                        foreach (DataRow row in dataTable.Rows)
                        {
                            rowNumber++; // Her döngüde satır numarasını artır

                            string serviceTag = row.Table.Columns.Contains("Device Service Tag") ? row["Device Service Tag"]?.ToString()?.Trim() : null;
                            if (string.IsNullOrEmpty(serviceTag))
                            {
                                skippedRows.Add($"Satır {rowNumber}: 'Device Service Tag' sütunu boş olduğu için atlandı.");
                                continue;
                            }

                            if (serversDict.TryGetValue(serviceTag, out Server server))
                            {
                                PortTipiEnum portTipi = default;
                                string turuStr = row.Table.Columns.Contains("Türü") ? row["Türü"]?.ToString() : "";
                                if (!string.IsNullOrEmpty(turuStr))
                                {
                                    Enum.TryParse<PortTipiEnum>(turuStr.Replace(" ", ""), true, out portTipi);
                                }

                                if (portTipi == default(PortTipiEnum))
                                {
                                    skippedRows.Add($"Satır {rowNumber}: Geçersiz veya boş 'Türü' değeri ('{turuStr}') için atlandı.");
                                    continue;
                                }

                                var port = new PortDetay { /* ... alan atamaları ... */ };
                                port.Aciklama = GeneratePortDescription(port, server);
                                portsToImport.Add(port);
                            }
                            else
                            {
                                skippedRows.Add($"Satır {rowNumber}: '{serviceTag}' Service Tag'ine sahip sunucu bulunamadığı için atlandı.");
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
                if (portsToImport.Any())
                {
                    await _context.PortDetaylari.AddRangeAsync(portsToImport);
                    await _context.SaveChangesAsync();
                }

                // Detaylı mesaj oluşturma
                var messageBuilder = new StringBuilder();
                messageBuilder.Append($"{portsToImport.Count} port başarıyla yüklendi.");
                if (skippedRows.Any())
                {
                    // \n karakterini <br> etiketine çevirerek HTML'de alt satıra geçmeyi sağlıyoruz
                    string skippedRowsHtml = string.Join("<br>", skippedRows.Select(s => "- " + s));
                    TempData["WarningMessage"] = $"{messageBuilder.ToString()} <hr><strong>Ancak {skippedRows.Count} satır şu sebeplerle atlandı:</strong><br>{skippedRowsHtml}";
                }
                else
                {
                    TempData["SuccessMessage"] = messageBuilder.ToString();
                }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = $"Veritabanına kayıt sırasında hata: {ex.InnerException?.Message ?? ex.Message}"; }
            return RedirectToAction(nameof(ListAll));
        }

        private string GeneratePortDescription(PortDetay port, Server server)
        {
            string deviceName = server.HostDns ?? "UnknownServer";
            string macAddress = "";
            if (port.PortTipi == PortTipiEnum.Bakir || port.PortTipi == PortTipiEnum.VirtualBakir) macAddress = port.BakirMAC;
            else if (port.PortTipi == PortTipiEnum.FC || port.PortTipi == PortTipiEnum.VirtualFC) macAddress = port.FiberMAC;
            else if (port.PortTipi == PortTipiEnum.FiberForSAN) macAddress = port.Wwpn;
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
        // GÜNCELLENMİŞ DETAILS METODU
        [Authorize(Roles = "SuperUser,User")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var portDetay = await _context.PortDetaylari
                .Include(p => p.Server) // Kaynak sunucu bilgisini getir
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (portDetay == null) return NotFound();

            // Hedef cihazı (switch) bulmak için SwName'i kullanarak Servers tablosunda arama yap
            if (!string.IsNullOrEmpty(portDetay.SwName))
            {
                var targetDevice = await _context.Servers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.HostDns == portDetay.SwName);

                // Bulunan hedef cihazı View'a göndermek için ViewData kullan
                ViewData["TargetDevice"] = targetDevice;
            }

            return View(portDetay);
        }
        [Authorize(Roles = "SuperUser,User")]
        public async Task<IActionResult> ListAll(PortFilterViewModel filterModel, string sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["ServerSortParm"] = String.IsNullOrEmpty(sortOrder) ? "server_desc" : "";
            ViewData["LocationSortParm"] = sortOrder == "location" ? "location_desc" : "location";
            ViewData["ModelSortParm"] = sortOrder == "model" ? "model_desc" : "model";
            ViewData["ServiceTagSortParm"] = sortOrder == "servicetag" ? "servicetag_desc" : "servicetag";
            ViewData["PortTypeSortParm"] = sortOrder == "porttype" ? "porttype_desc" : "porttype";

            IQueryable<PortDetay> portsQuery = _context.PortDetaylari.Include(p => p.Server);

            if (filterModel.SelectedLocations != null && filterModel.SelectedLocations.Any())
                portsQuery = portsQuery.Where(p => filterModel.SelectedLocations.Contains(p.Server.Location));
            if (filterModel.SelectedPortTypes != null && filterModel.SelectedPortTypes.Any())
            {
                var selectedTypesAsEnum = filterModel.SelectedPortTypes.Select(s => Enum.Parse<PortTipiEnum>(s)).ToList();
                portsQuery = portsQuery.Where(p => selectedTypesAsEnum.Contains(p.PortTipi));
            }
            if (filterModel.SelectedLinkStatuses != null && filterModel.SelectedLinkStatuses.Any())
                portsQuery = portsQuery.Where(p => filterModel.SelectedLinkStatuses.Contains(p.LinkStatus));
            if (filterModel.SelectedSwNames != null && filterModel.SelectedSwNames.Any())
                portsQuery = portsQuery.Where(p => filterModel.SelectedSwNames.Contains(p.SwName));

            switch (sortOrder)
            {
                case "server_desc": portsQuery = portsQuery.OrderByDescending(p => p.Server.HostDns); break;
                case "location": portsQuery = portsQuery.OrderBy(p => p.Server.Location); break;
                case "location_desc": portsQuery = portsQuery.OrderByDescending(p => p.Server.Location); break;
                case "model": portsQuery = portsQuery.OrderBy(p => p.Server.Model); break;
                case "model_desc": portsQuery = portsQuery.OrderByDescending(p => p.Server.Model); break;
                case "servicetag": portsQuery = portsQuery.OrderBy(p => p.Server.ServiceTag); break;
                case "servicetag_desc": portsQuery = portsQuery.OrderByDescending(p => p.Server.ServiceTag); break;
                case "porttype": portsQuery = portsQuery.OrderBy(p => p.PortTipi); break;
                case "porttype_desc": portsQuery = portsQuery.OrderByDescending(p => p.PortTipi); break;
                default: portsQuery = portsQuery.OrderBy(p => p.Server.HostDns); break;
            }

            var filteredPorts = await portsQuery.AsNoTracking().ToListAsync();

            var portTypes = Enum.GetValues(typeof(PortTipiEnum))
                                .Cast<PortTipiEnum>()
                                .Select(e => new SelectListItem { Text = e.ToString(), Value = e.ToString() })
                                .ToList();

            var viewModel = new PortFilterViewModel
            {
                Ports = filteredPorts,
                SelectedLocations = filterModel.SelectedLocations,
                SelectedPortTypes = filterModel.SelectedPortTypes,
                SelectedLinkStatuses = filterModel.SelectedLinkStatuses,
                SelectedSwNames = filterModel.SelectedSwNames,
                AllLocations = await _context.Servers.Where(s => !string.IsNullOrEmpty(s.Location)).Select(s => s.Location).Distinct().Select(l => new SelectListItem(l, l)).ToListAsync(),
                AllPortTypes = portTypes,
                AllLinkStatuses = await _context.PortDetaylari.Where(p => !string.IsNullOrEmpty(p.LinkStatus)).Select(p => p.LinkStatus).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync(),
                AllSwNames = await _context.PortDetaylari.Where(p => !string.IsNullOrEmpty(p.SwName)).Select(p => p.SwName).Distinct().Select(sw => new SelectListItem(sw, sw)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportToExcel(ExportViewModel model)
        {
            var selectedColumns = model.Columns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
            if (!selectedColumns.Any())
            {
                TempData["ErrorMessage"] = "Lütfen dışarı aktarmak için en az bir sütun seçin.";
                return RedirectToAction(nameof(ListAll));
            }

            // Portları çekerken ilişkili sunucu bilgisini de getiriyoruz
            var ports = await _context.PortDetaylari.Include(p => p.Server).AsNoTracking().ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Port_Listesi");
                var currentRow = 1;

                // Başlıkları oluştur
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    worksheet.Cell(currentRow, i + 1).Value = selectedColumns[i];
                }

                // Verileri ekle
                foreach (var port in ports)
                {
                    currentRow++;
                    for (int i = 0; i < selectedColumns.Count; i++)
                    {
                        var columnName = selectedColumns[i];
                        object propertyValue = null;

                        // Eğer sütun adı "Server" ise, ilişkili sunucunun HostDns'ini al
                        if (columnName == "Server")
                        {
                            propertyValue = port.Server?.HostDns;
                        }
                        else
                        {
                            propertyValue = typeof(PortDetay).GetProperty(columnName)?.GetValue(port, null);
                        }

                        worksheet.Cell(currentRow, i + 1).Value = propertyValue?.ToString() ?? "";
                    }
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string excelName = $"Port_Listesi_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                }
            }
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