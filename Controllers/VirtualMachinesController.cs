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
using ClosedXML.Excel; 

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

            // YENİ POOL FİLTRESİ
            if (filterModel.SelectedPools != null && filterModel.SelectedPools.Any())
                vmsQuery = vmsQuery.Where(vm => filterModel.SelectedPools.Contains(vm.Pool));

            var filteredVms = await vmsQuery.OrderBy(vm => vm.VipIp).ThenBy(vm => vm.Dns).ToListAsync();

            var viewModel = new VirtualMachineFilterViewModel
            {
                VirtualMachines = filteredVms,
                SelectedStatuses = filterModel.SelectedStatuses,
                SelectedNetworks = filterModel.SelectedNetworks,
                SelectedHosts = filterModel.SelectedHosts,
                SelectedClusters = filterModel.SelectedClusters,
                SelectedPools = filterModel.SelectedPools, // YENİ

                AllStatuses = await _context.VirtualMachines.Where(vm => !string.IsNullOrEmpty(vm.Status)).Select(vm => vm.Status).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync(),
                AllNetworks = await _context.VirtualMachines.Where(vm => !string.IsNullOrEmpty(vm.Network)).Select(vm => vm.Network).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync(),
                AllHosts = await _context.VirtualMachines.Where(vm => !string.IsNullOrEmpty(vm.Host)).Select(vm => vm.Host).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync(),
                AllClusters = await _context.VirtualMachines.Where(vm => !string.IsNullOrEmpty(vm.Cluster)).Select(vm => vm.Cluster).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync(),
                // YENİ POOL LİSTESİ
                AllPools = await _context.VirtualMachines.Where(vm => !string.IsNullOrEmpty(vm.Pool)).Select(vm => vm.Pool).Distinct().Select(s => new SelectListItem(s, s)).ToListAsync()
            };

            var allServers = await _context.Servers.Where(s => s.HostDns != null).ToListAsync();
            var serverHostMap = allServers.GroupBy(s => s.HostDns).ToDictionary(g => g.Key, g => g.First().Id);
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportToExcel(ExportViewModel model)
        {
            var selectedColumns = model.Columns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
            if (!selectedColumns.Any())
            {
                TempData["ErrorMessage"] = "Lütfen dışarı aktarmak için en az bir sütun seçin.";
                return RedirectToAction(nameof(Index));
            }

            var virtualMachines = await _context.VirtualMachines.AsNoTracking().ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sanal_Makineler");
                var currentRow = 1;

                // Başlıkları oluştur
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    worksheet.Cell(currentRow, i + 1).Value = selectedColumns[i];
                }

                // Verileri ekle
                foreach (var vm in virtualMachines)
                {
                    currentRow++;
                    for (int i = 0; i < selectedColumns.Count; i++)
                    {
                        var propertyValue = typeof(VirtualMachine).GetProperty(selectedColumns[i])?.GetValue(vm, null);
                        worksheet.Cell(currentRow, i + 1).Value = propertyValue?.ToString() ?? "";
                    }
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string excelName = $"Sanal_Makineler_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                }
            }
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