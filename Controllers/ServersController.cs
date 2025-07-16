// Konum: Controllers/ServersController.cs
using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace BelbimEnv.Controllers
{
    public class ServersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ServersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index() => View(await _context.Servers.ToListAsync());
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var server = await _context.Servers.FirstOrDefaultAsync(m => m.Id == id);
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
                    var serverFromDb = await _context.Servers.FindAsync(id);
                    if (serverFromDb == null) return NotFound();

                    serverFromDb.HostDns = serverFromForm.HostDns;
                    serverFromDb.IpAdress = serverFromForm.IpAdress;
                    serverFromDb.Model = serverFromForm.Model;
                    serverFromDb.ServiceTag = serverFromForm.ServiceTag;
                    serverFromDb.VcenterAdress = serverFromForm.VcenterAdress;
                    serverFromDb.Cluster = serverFromForm.Cluster;
                    serverFromDb.Location = serverFromForm.Location;
                    serverFromDb.OS = serverFromForm.OS;
                    serverFromDb.IloIdracIp = serverFromForm.IloIdracIp;
                    serverFromDb.Kabin = serverFromForm.Kabin;
                    serverFromDb.RearFront = serverFromForm.RearFront;
                    serverFromDb.KabinU = serverFromForm.KabinU;
                    serverFromDb.IsttelkomEtiketId = serverFromForm.IsttelkomEtiketId;
                    serverFromDb.LastUpdated = DateTime.Now;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException) { throw; }
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
            if (server != null) _context.Servers.Remove(server);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
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
                if (importOption == "replace") _context.Servers.RemoveRange(_context.Servers);
                await _context.Servers.AddRangeAsync(serversToImport);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{serversToImport.Count} kayıt başarıyla yüklendi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Veritabanı hatası: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
        public class ServerMap : ClassMap<Server>
        {
            public ServerMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.Id).Ignore();
                Map(m => m.DateAdded).Ignore();
                Map(m => m.LastUpdated).Ignore();
            }
        }
    }
}