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
using ClosedXML.Excel; // Yeni Excel dosyaları oluşturmak için gerekli

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
            var tempFilePaths = new List<string>();

            try
            {
                var originalTempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx");
                tempFilePaths.Add(originalTempFilePath);
                using (var fileStream = new FileStream(originalTempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                DataTable sourceDataTable;
                using (var stream = System.IO.File.Open(originalTempFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true } });
                        sourceDataTable = result.Tables[0];
                    }
                }

                int totalRows = sourceDataTable.Rows.Count;
                int midpoint = totalRows / 2;

                var dataTables = new List<DataTable> { sourceDataTable.Clone(), sourceDataTable.Clone() };
                for (int i = 0; i < midpoint; i++) { dataTables[0].ImportRow(sourceDataTable.Rows[i]); }
                for (int i = midpoint; i < totalRows; i++) { dataTables[1].ImportRow(sourceDataTable.Rows[i]); }

                for (int i = 0; i < dataTables.Count; i++)
                {
                    var tempPartFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + $"_part{i + 1}.xlsx");
                    tempFilePaths.Add(tempPartFilePath);
                    using (var workbook = new XLWorkbook())
                    {
                        workbook.Worksheets.Add(dataTables[i], "Data");
                        workbook.SaveAs(tempPartFilePath);
                    }
                }

                var allPortsToImport = new List<PortDetay>();
                var allSkippedRows = new List<string>();

                var serversList = await _context.Servers.Where(s => !string.IsNullOrEmpty(s.ServiceTag)).ToListAsync();
                var serversDict = serversList
                    .GroupBy(s => s.ServiceTag.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.First().ServiceTag.Trim(), g => g.First(), StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < 2; i++)
                {
                    var partFilePath = tempFilePaths[i + 1];
                    var partDataTable = new DataTable();
                    using (var stream = System.IO.File.Open(partFilePath, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            var result = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true } });
                            partDataTable = result.Tables[0];
                        }
                    }
                    ProcessDataTable(partDataTable, serversDict, allPortsToImport, allSkippedRows, i * midpoint);
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (importOption == "replace")
                        {
                            await _context.PortDetaylari.ExecuteDeleteAsync();
                        }

                        if (allPortsToImport.Any())
                        {
                            int batchSize = 100;
                            for (int i = 0; i < allPortsToImport.Count; i += batchSize)
                            {
                                var batch = allPortsToImport.Skip(i).Take(batchSize).ToList();
                                await _context.PortDetaylari.AddRangeAsync(batch);
                                await _context.SaveChangesAsync();
                            }
                        }

                        await transaction.CommitAsync();

                        var messageBuilder = new StringBuilder();
                        if (allPortsToImport.Any()) { messageBuilder.Append($"{allPortsToImport.Count} port başarıyla yüklendi."); }
                        if (allSkippedRows.Any())
                        {
                            string skippedRowsHtml = string.Join("<br>", allSkippedRows.Select(s => "- " + System.Net.WebUtility.HtmlEncode(s)));
                            if (allPortsToImport.Any()) { TempData["WarningMessage"] = $"{messageBuilder.ToString()} <hr><strong>Ancak, {allSkippedRows.Count} satır atlandı:</strong><br>{skippedRowsHtml}"; }
                            else { TempData["ErrorMessage"] = $"Hiçbir port eklenemedi. <hr><strong>Tüm satırlar atlandı:</strong><br>{skippedRowsHtml}"; }
                        }
                        else if (allPortsToImport.Any()) { TempData["SuccessMessage"] = messageBuilder.ToString(); }
                        else { TempData["WarningMessage"] = "Excel dosyasında işlenecek geçerli veri bulunamadı."; }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"Veritabanına kayıt sırasında kritik bir hata oluştu ve tüm işlem geri alındı: {ex.InnerException?.Message ?? ex.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Excel dosyası işlenirken hata: {ex.Message}.";
            }
            finally
            {
                foreach (var path in tempFilePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }
            }

            return RedirectToAction(nameof(ListAll));
        }

        // === EKSİK OLMAYAN, TAM YARDIMCI METOTLAR BURADA ===
        private void ProcessDataTable(DataTable dataTable, Dictionary<string, Server> serversDict, List<PortDetay> portsToImport, List<string> skippedRows, int startingRowNumber)
        {
            int rowNumber = startingRowNumber;
            foreach (DataRow row in dataTable.Rows)
            {
                rowNumber++;
                try
                {
                    if (row.ItemArray.All(x => x == null || x is DBNull || string.IsNullOrWhiteSpace(x.ToString()))) { continue; }

                    string serviceTag = row.Table.Columns.Contains("Device Service Tag") ? row["Device Service Tag"]?.ToString()?.Trim() : null;
                    if (string.IsNullOrEmpty(serviceTag))
                    {
                        skippedRows.Add($"Satır {rowNumber}: 'Device Service Tag' sütunu boş, atlandı.");
                        continue;
                    }

                    if (serversDict.TryGetValue(serviceTag, out Server server))
                    {
                        string turuStr = row.Table.Columns.Contains("Türü") ? row["Türü"]?.ToString()?.Trim() : "";
                        if (string.IsNullOrEmpty(turuStr) || !Enum.TryParse<PortTipiEnum>(turuStr.Replace(" ", ""), true, out PortTipiEnum portTipi) || portTipi == default(PortTipiEnum))
                        {
                            skippedRows.Add($"Satır {rowNumber}: 'Türü' sütunu boş veya geçersiz ('{turuStr}'), atlandı.");
                            continue;
                        }

                        var port = new PortDetay
                        {
                            ServerId = server.Id,
                            PortTipi = portTipi,
                            LinkStatus = row.Table.Columns.Contains("Link Status") ? row["Link Status"]?.ToString()?.Trim() : null,
                            LinkSpeed = row.Table.Columns.Contains("Link Speed") ? row["Link Speed"]?.ToString()?.Trim() : null,
                            PortId = row.Table.Columns.Contains("Port ID") ? row["Port ID"]?.ToString()?.Trim() : null,
                            NicId = row.Table.Columns.Contains("NIC ID") ? row["NIC ID"]?.ToString()?.Trim() : null,
                            FiberMAC = row.Table.Columns.Contains("Fiber MAC") ? row["Fiber MAC"]?.ToString()?.Trim() : null,
                            BakirMAC = row.Table.Columns.Contains("Bakır MAC") ? row["Bakır MAC"]?.ToString()?.Trim() : null,
                            Wwpn = row.Table.Columns.Contains("WWPN") ? row["WWPN"]?.ToString()?.Trim() : null,
                            SwName = row.Table.Columns.Contains("SW NAME") ? row["SW NAME"]?.ToString()?.Trim() : null,
                            SwPort = row.Table.Columns.Contains("SW PORT") ? row["SW PORT"]?.ToString()?.Trim() : null,
                            Description = row.Table.Columns.Contains("Description Yeni") ? row["Description Yeni"]?.ToString()?.Trim() : null
                        };

                        port.Aciklama = GeneratePortDescription(port, server);
                        portsToImport.Add(port);
                    }
                    else
                    {
                        skippedRows.Add($"Satır {rowNumber}: '{serviceTag}' Service Tag'ine sahip sunucu bulunamadı, atlandı.");
                    }
                }
                catch (Exception ex)
                {
                    skippedRows.Add($"Satır {rowNumber}: İşlenemedi. Hata: {ex.Message}");
                }
            }
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