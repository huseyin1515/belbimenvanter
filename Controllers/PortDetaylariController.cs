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
        public PortDetaylariController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Manage(int? id)
        {
            if (id == null) return NotFound();
            var server = await _context.Servers.Include(s => s.PortDetaylari).FirstOrDefaultAsync(s => s.Id == id);
            if (server == null) return NotFound();

            var viewModel = new PortDetayViewModel
            {
                ServerId = server.Id,
                DeviceName = server.HostDns,
                Portlar = server.PortDetaylari.ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(PortDetayViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);

            var existingPorts = _context.PortDetaylari.Where(p => p.ServerId == viewModel.ServerId);
            _context.PortDetaylari.RemoveRange(existingPorts);

            if (viewModel.Portlar != null)
            {
                foreach (var port in viewModel.Portlar)
                {
                    if (!string.IsNullOrEmpty(port.PortTipi))
                    {
                        port.ServerId = viewModel.ServerId;
                        port.Id = 0;
                        _context.PortDetaylari.Add(port);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Servers");
        }
    }
}