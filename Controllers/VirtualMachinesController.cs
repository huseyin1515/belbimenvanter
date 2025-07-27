using BelbimEnv.Data;
using BelbimEnv.Models;
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
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    [Authorize]
    public class VirtualMachinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VirtualMachinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GÜNCELLENMİŞ INDEX METODU
        public async Task<IActionResult> Index(VirtualMachineFilterViewModel filterModel)
        {
            IQueryable<VirtualMachine> vmsQuery = _context.VirtualMachines.AsQueryable();

            // Filtreleri uygula
            if (filterModel.SelectedStatuses != null && filterModel.SelectedStatuses.Any())
                vmsQuery = vmsQuery.Where(vm => filterModel.SelectedStatuses.Contains(vm.Status));
            if (filterModel.SelectedNetworks != null && filterModel.SelectedNetworks.Any())
                vmsQuery = vmsQuery.Where(vm => filterModel.SelectedNetworks.Contains(vm.Network));
            if (filterModel.SelectedHosts != null && filterModel.SelectedHosts.Any())
                vmsQuery = vmsQuery.Where(vm => filterModel.SelectedHosts.Contains(vm.Host));
            if (filterModel.SelectedClusters != null && filterModel.SelectedClusters.Any())
                vmsQuery = vmsQuery.Where(vm => filterModel.SelectedClusters.Contains(vm.Cluster));

            var filteredVms = await vmsQuery.OrderBy(vm => vm.VipIp).ThenBy(vm => vm.Dns).ToListAsync();

            // ViewModel'ı doldur
            var viewModel = new VirtualMachineFilterViewModel
            {
                VirtualMachines = filteredVms,
                SelectedStatuses = filterModel.SelectedStatuses,
                SelectedNetworks = filterModel.SelectedNetworks,
                SelectedHosts = filterModel.SelectedHosts,
                SelectedClusters = filterModel.SelectedClusters,

                // Filtre seçenekleri için tüm benzersiz değerleri al
                AllStatuses = await _context.VirtualMachines.Where(vm => vm.Status != null).Select(vm => vm.Status).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync(),
                AllNetworks = await _context.VirtualMachines.Where(vm => vm.Network != null).Select(vm => vm.Network).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync(),
                AllHosts = await _context.VirtualMachines.Where(vm => vm.Host != null).Select(vm => vm.Host).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync(),
                AllClusters = await _context.VirtualMachines.Where(vm => vm.Cluster != null).Select(vm => vm.Cluster).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync()
            };

            var serverHostMap = await _context.Servers.Where(s => s.HostDns != null).GroupBy(s => s.HostDns).ToDictionaryAsync(g => g.Key, g => g.First().Id);
            ViewData["ServerHostMap"] = serverHostMap;

            return View(viewModel);
        }

        // GET: /VirtualMachines/Details/5
        [Authorize(Roles = "SuperUser,User")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var vm = await _context.VirtualMachines.FirstOrDefaultAsync(m => m.Id == id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // GET: /VirtualMachines/Create
        [Authorize(Roles = "SuperUser")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Create([Bind("VmName,Dns,MachineName,MachineIp,Host,OS,Pool,Status")] VirtualMachine vm)
        {
            if (ModelState.IsValid)
            {
                vm.DateAdded = DateTime.Now;
                vm.LastUpdated = DateTime.Now;
                _context.Add(vm);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Yeni sanal makine başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // GET: /VirtualMachines/Edit/5
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var vm = await _context.VirtualMachines.FindAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SN,VmName,Dns,VipIp,MachineIp,Port,MachineName,Status,Network,Cluster,Host,OS,Pool,DateAdded")] VirtualMachine vm)
        {
            if (id != vm.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    vm.LastUpdated = DateTime.Now;
                    _context.Update(vm);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sanal makine bilgileri başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException) { if (!_context.VirtualMachines.Any(e => e.Id == vm.Id)) return NotFound(); else throw; }
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // GET: /VirtualMachines/Delete/5
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var vm = await _context.VirtualMachines.FirstOrDefaultAsync(m => m.Id == id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUser")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vm = await _context.VirtualMachines.FindAsync(id);
            if (vm != null)
            {
                _context.VirtualMachines.Remove(vm);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sanal makine başarıyla silindi.";
            }
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ImportExcel(IFormFile file, string importOption)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen bir Excel (.xlsx) dosyası seçin.";
                return RedirectToAction(nameof(Index));
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var vmsToImport = new List<VirtualMachine>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true } });
                        DataTable dataTable = result.Tables[0];

                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (row["Ham"] == null || string.IsNullOrEmpty(row["Ham"].ToString())) continue;

                            var vm = new VirtualMachine
                            {
                                SN = row["SN"]?.ToString(),
                                VmName = row["Ham"]?.ToString(),
                                Dns = row["dns"]?.ToString(),
                                VipIp = row["vip ip"]?.ToString(),
                                Port = row["Port"]?.ToString(),
                                MachineIp = row["Makine Ip"]?.ToString(),
                                MachineName = row["Makine Adı"]?.ToString(),
                                Status = row["Durumu"]?.ToString(),
                                Network = row["Network"]?.ToString(),
                                Cluster = row["Cluster"]?.ToString(),
                                Host = row["Host"]?.ToString(),
                                OS = row["OS"]?.ToString(),
                                Pool = row["Pool"]?.ToString(),
                                DateAdded = DateTime.Now,
                                LastUpdated = DateTime.Now
                            };
                            vmsToImport.Add(vm);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Excel dosyası okunurken hata: {ex.Message}. Sütun başlıklarını kontrol edin.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (importOption == "replace")
                {
                    _context.VirtualMachines.RemoveRange(_context.VirtualMachines);
                }
                await _context.VirtualMachines.AddRangeAsync(vmsToImport);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{vmsToImport.Count} sanal makine kaydı başarıyla yüklendi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Veritabanına kayıt sırasında hata: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}