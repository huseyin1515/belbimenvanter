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
        // YARDIMCI METOT: Açıklama metnini oluşturur (DÜZELTİLDİ)
        //==================================================================
        private string GeneratePortDescription(PortDetay port, Server server)
        {
            // 1. Cihaz Adı (Device Name)
            string deviceName = server.HostDns ?? "UnknownServer";

            // 2. İlgili MAC/WWPN adresini bul
            string macAddress = "";
            if (port.PortTipi == PortTipiEnum.Bakir) macAddress = port.BakirMAC;
            else if (port.PortTipi == PortTipiEnum.FC || port.PortTipi == PortTipiEnum.VirtualFC) macAddress = port.FiberMAC;
            else if (port.PortTipi == PortTipiEnum.SAN) macAddress = port.Wwpn;

            // 3. MAC adresinin son 4 hanesini al
            string lastFourDigits = "XXXX"; // Varsayılan değer
            if (!string.IsNullOrEmpty(macAddress))
            {
                // Aradaki iki nokta ve tireleri kaldırıp son 4 haneyi alıyoruz
                string cleanMac = new string(macAddress.Where(char.IsLetterOrDigit).ToArray());
                if (cleanMac.Length >= 4)
                {
                    // DÜZELTME: TakeLast bir karakter dizisi döndürür, bunu new string() ile metne çeviriyoruz.
                    lastFourDigits = new string(cleanMac.TakeLast(4).ToArray()).ToUpper();
                }
            }

            // 4. Türünün Baş Harfi
            string typeInitial = port.PortTipi.ToString().ToUpper().FirstOrDefault().ToString();

            // 5. Uplink Port / FC Uç Port Sayısı
            string countInfo = "0"; // Varsayılan değer
            if (port.PortTipi == PortTipiEnum.Bakir && !string.IsNullOrEmpty(port.BakirUplinkPort))
            {
                countInfo = port.BakirUplinkPort;
            }
            else if ((port.PortTipi == PortTipiEnum.FC || port.PortTipi == PortTipiEnum.VirtualFC) && port.FcUcPortSayisi.HasValue)
            {
                countInfo = port.FcUcPortSayisi.Value.ToString();
            }

            return $"{deviceName}_{lastFourDigits}_{typeInitial}_{countInfo}";
        }


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

        // POST: /PortDetaylari/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PortDetay YeniPortFormu)
        {
            if (YeniPortFormu.ServerId == 0) return BadRequest("Sunucu ID'si form verisinde bulunamadı.");

            var server = await _context.Servers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == YeniPortFormu.ServerId);
            if (server == null) return NotFound("Portun ekleneceği sunucu bulunamadı.");

            if (ModelState.IsValid)
            {
                YeniPortFormu.Aciklama = GeneratePortDescription(YeniPortFormu, server);
                _context.Add(YeniPortFormu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Yeni port başarıyla eklendi.";
            }
            else
            {
                var errorMessages = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = "Formda hatalar var: " + string.Join(" ", errorMessages);
            }
            return RedirectToAction(nameof(Manage), new { id = YeniPortFormu.ServerId });
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
            if (portDetay == null)
            {
                TempData["ErrorMessage"] = "Silinecek port bulunamadı.";
                return RedirectToAction("Index", "Servers");
            }
            int serverId = portDetay.ServerId;
            _context.PortDetaylari.Remove(portDetay);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Port başarıyla silindi.";
            return RedirectToAction("Details", "Servers", new { id = serverId });
        }
    }
}