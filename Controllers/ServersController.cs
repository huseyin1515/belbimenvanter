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
using ExcelDataReader; // Excel okumak için eklendi
using System.Data;     // DataTable kullanmak için eklendi

namespace BelbimEnv.Controllers
{
    public class ServersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ServersController(ApplicationDbContext context) { _context = context; }

        //==================================================================
        // MEVCUT CRUD METOTLARI
        //==================================================================

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

        // ====================================================================
        // === EXCEL (.xlsx) YÜKLEME METODU (TAMAMEN YENİDEN YAZILDI) ===
        // ====================================================================

        [HttpPost]
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
                            if (row["Host DNS"] == null || string.IsNullOrEmpty(row["Host DNS"].ToString()))
                            {
                                continue;
                            }

                            var server = new Server
                            {
                                HostDns = row["Host DNS"]?.ToString(),
                                IpAdress = row["IP Adress"]?.ToString(),
                                Model = row["Model"]?.ToString(),
                                ServiceTag = row["Service Tag / Serial Number"]?.ToString(),
                                VcenterAdress = row["Vcenter Adress"]?.ToString(),
                                Cluster = row["Cluster"]?.ToString(),
                                Location = row["Location"]?.ToString(),
                                OS = row["o/s"]?.ToString(),
                                IloIdracIp = row["ilo/idrac ip"]?.ToString(),
                                Kabin = row["KABİN"]?.ToString(),
                                RearFront = row["Rear/Front"]?.ToString(),
                                KabinU = int.TryParse(row["KABİN U"]?.ToString(), out int kabinU) ? kabinU : null,
                                IsttelkomEtiketId = row["İsttelkom Etiket ID"]?.ToString(),
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