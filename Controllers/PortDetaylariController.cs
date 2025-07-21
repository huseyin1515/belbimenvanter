using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    public class PortDetaylariController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PortDetaylariController(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================================
        // YARDIMCI METOT: Açıklama metnini oluşturur.
        //==================================================================
        private string GeneratePortDescription(PortDetay port, Server server)
        {
            string deviceName = server.HostDns ?? "UnknownServer";
            string macAddress = "";
            if (port.PortTipi == PortTipiEnum.Bakir) macAddress = port.BakirMAC;
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
            if (port.PortTipi == PortTipiEnum.Bakir && !string.IsNullOrEmpty(port.BakirUplinkPort)) { countInfo = port.BakirUplinkPort; }
            else if ((port.PortTipi == PortTipiEnum.FC || port.PortTipi == PortTipiEnum.VirtualFC) && port.FcUcPortSayisi.HasValue) { countInfo = port.FcUcPortSayisi.Value.ToString(); }
            return $"{deviceName}_{lastFourDigits}_{typeInitial}_{countInfo}";
        }

        //==================================================================
        // YENİ EKLENEN METOT: Bir portun tüm detaylarını gösterir.
        //==================================================================
        // GET: /PortDetaylari/Details/15
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var portDetay = await _context.PortDetaylari
                .Include(p => p.Server) // Ait olduğu sunucu bilgisini de getir
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (portDetay == null)
            {
                return NotFound();
            }

            return View(portDetay);
        }

        //==================================================================
        // MEVCUT METOTLAR
        //==================================================================

        // GET: /PortDetaylari/ListAll
        public async Task<IActionResult> ListAll()
        {
            var allPorts = await _context.PortDetaylari.Include(p => p.Server).AsNoTracking().OrderByDescending(p => p.Id).ToListAsync();
            return View(allPorts);
        }

        // GET: /PortDetaylari/Manage/5
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

        // GET: /PortDetaylari/Create?serverId=5
        public async Task<IActionResult> Create(int serverId)
        {
            var server = await _context.Servers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null) return NotFound("Port eklenecek sunucu bulunamadı.");
            var viewModel = new PortCreateBulkViewModel { ServerId = serverId, ServerName = server.HostDns };
            return View(viewModel);
        }

        // POST: /PortDetaylari/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // GET: /PortDetaylari/Edit/15
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var portDetay = await _context.PortDetaylari.FindAsync(id);
            if (portDetay == null) return NotFound();
            return View(portDetay);
        }

        // POST: /PortDetaylari/Edit/15
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // POST: /PortDetaylari/Delete/15
        [HttpPost]
        [ValidateAntiForgeryToken]
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