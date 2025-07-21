using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using ExcelDataReader;
using System.Data;

namespace BelbimEnv.Controllers
{
    public class ServersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ServersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var servers = await _context.Servers.OrderBy(s => s.Id).ToListAsync();
            return View(servers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var server = await _context.Servers.Include(s => s.PortDetaylari).FirstOrDefaultAsync(m => m.Id == id);
            if (server == null) return NotFound();
            return View(server);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Server server)
        {
            if (ModelState.IsValid)
            {
                server.DateAdded = DateTime.Now;
                server.LastUpdated = DateTime.Now;
                _context.Add(server);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Yeni sunucu başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(server);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var server = await _context.Servers.FindAsync(id);
            if (server == null) return NotFound();
            return View(server);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Server serverFromForm)
        {
            if (id != serverFromForm.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    var serverFromDb = await _context.Servers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                    if (serverFromDb == null) return NotFound();
                    serverFromForm.DateAdded = serverFromDb.DateAdded;
                    serverFromForm.LastUpdated = DateTime.Now;
                    _context.Update(serverFromForm);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sunucu bilgileri başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Servers.Any(e => e.Id == serverFromForm.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(serverFromForm);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var server = await _context.Servers.FirstOrDefaultAsync(m => m.Id == id);
            if (server == null) return NotFound();
            return View(server);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var server = await _context.Servers.FindAsync(id);
            if (server != null)
            {
                var relatedPorts = _context.PortDetaylari.Where(p => p.ServerId == id);
                _context.PortDetaylari.RemoveRange(relatedPorts);
                _context.Servers.Remove(server);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sunucu ve ilişkili portları başarıyla silindi.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file, string importOption)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen bir Excel (.xlsx) dosyası seçin.";
                return RedirectToAction(nameof(Index));
            }

            if (Path.GetExtension(file.FileName).ToLower() != ".xlsx")
            {
                TempData["ErrorMessage"] = "Lütfen sadece .xlsx uzantılı Excel dosyası yükleyin.";
                return RedirectToAction(nameof(Index));
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var serversToImport = new List<Server>();
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                        });

                        DataTable dataTable = result.Tables[0];

                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (!row.Table.Columns.Contains("Host DNS") || row["Host DNS"] == null || string.IsNullOrEmpty(row["Host DNS"].ToString()))
                            {
                                continue;
                            }

                            var server = new Server
                            {
                                HostDns = row["Host DNS"]?.ToString(),
                                IpAdress = row.Table.Columns.Contains("IP Adress") ? row["IP Adress"]?.ToString() : null,
                                Model = row.Table.Columns.Contains("Model") ? row["Model"]?.ToString() : null,
                                ServiceTag = row.Table.Columns.Contains("Service Tag / Serial Number") ? row["Service Tag / Serial Number"]?.ToString() : null,
                                VcenterAdress = row.Table.Columns.Contains("Vcenter Adress") ? row["Vcenter Adress"]?.ToString() : null,
                                Cluster = row.Table.Columns.Contains("Cluster") ? row["Cluster"]?.ToString() : null,
                                Location = row.Table.Columns.Contains("Location") ? row["Location"]?.ToString() : null,
                                OS = row.Table.Columns.Contains("o/s") ? row["o/s"]?.ToString() : null,
                                IloIdracIp = row.Table.Columns.Contains("ilo/idrac ip") ? row["ilo/idrac ip"]?.ToString() : null,

                                // DÜZELTME: Doğru sütunlar doğru özelliklere atanıyor.
                                Kabin = row.Table.Columns.Contains("_akabin") ? row["_akabin"]?.ToString() : null,
                                KabinU = row.Table.Columns.Contains("kabin_u") ? row["kabin_u"]?.ToString() : null,

                                RearFront = row.Table.Columns.Contains("Rear/Front") ? row["Rear/Front"]?.ToString() : null,
                                IsttelkomEtiketId = row.Table.Columns.Contains("İsttelkom Etiket I") ? row["İsttelkom Etiket I"]?.ToString() : null,

                                DateAdded = DateTime.Now,
                                LastUpdated = DateTime.Now
                            };
                            serversToImport.Add(server);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Excel dosyası okunurken hata: {ex.Message}. Lütfen dosya formatını ve sütun başlıklarını kontrol edin.";
                return RedirectToAction(nameof(Index));
            }

            if (!serversToImport.Any())
            {
                TempData["ErrorMessage"] = "Excel dosyasında işlenecek geçerli veri bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (importOption == "replace")
                {
                    _context.PortDetaylari.RemoveRange(_context.PortDetaylari);
                    _context.Servers.RemoveRange(_context.Servers);
                    await _context.SaveChangesAsync();
                }

                await _context.Servers.AddRangeAsync(serversToImport);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{serversToImport.Count} kayıt başarıyla yüklendi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Veritabanına kayıt sırasında hata: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}