using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BelbimEnv.Controllers
{
    public class ServersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ServersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var servers = await _context.Servers.Include(s => s.PortDetaylari).ToListAsync();
            return View(servers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var server = await _context.Servers
                .Include(s => s.PortDetaylari)
                .FirstOrDefaultAsync(m => m.Id == id);
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
                _context.Servers.Remove(server);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ====================================================================
        // === YENİDEN EKLENEN CSV YÜKLEME METODU VE MAP SINIFI BAŞLANGIÇ ===
        // ====================================================================

        [HttpPost]
        public async Task<IActionResult> ImportCsv(IFormFile file, string importOption)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen bir CSV dosyası seçin.";
                return RedirectToAction(nameof(Index));
            }

            var serversToImport = new List<Server>();
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                csv.Context.RegisterClassMap<ServerMap>();
                serversToImport = csv.GetRecords<Server>().ToList();

                foreach (var server in serversToImport)
                {
                    server.DateAdded = DateTime.Now;
                    server.LastUpdated = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"CSV dosyası okunurken hata: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (importOption == "replace")
                {
                    // UYARI: Bu işlem tüm sunucuları ve onlara bağlı port detaylarını silecektir!
                    _context.Servers.RemoveRange(_context.Servers);
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

        // CSVHelper'ın Server nesnesini nasıl eşleyeceğini belirten sınıf.
        public class ServerMap : ClassMap<Server>
        {
            public ServerMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                // Veritabanı tarafından yönetilen veya CSV'de olmayan alanları görmezden gel.
                Map(m => m.Id).Ignore();
                Map(m => m.DateAdded).Ignore();
                Map(m => m.LastUpdated).Ignore();
                Map(m => m.PortDetaylari).Ignore();
            }
        }

        // ====================================================================
        // === YENİDEN EKLENEN CSV YÜKLEME METODU VE MAP SINIFI BİTİŞ ===
        // ====================================================================
    }
}