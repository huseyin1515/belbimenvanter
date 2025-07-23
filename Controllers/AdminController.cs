using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    [Authorize(Roles = "SuperUser")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> UserDetails(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // GET: /Admin/Index
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.OrderBy(u => u.Role).ThenBy(u => u.Username).ToListAsync();
            return View(users);
        }

        // GET: /Admin/EditUser/5
        public async Task<IActionResult> EditUser(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Roles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "SuperUser", Text = "Süper Kullanıcı" },
                    new SelectListItem { Value = "User", Text = "Kullanıcı" },
                    new SelectListItem { Value = "Bekleyen", Text = "Onay Bekliyor" }
                }
            };
            return View(viewModel);
        }

        // POST: /Admin/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, [Bind("Id,Role")] EditUserViewModel model)
        {
            if (id != model.Id) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Sadece rolü güncelle
            user.Role = model.Role;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{user.Username} adlı kullanıcının rolü güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/ApproveUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Role == "Bekleyen")
            {
                user.Role = "User";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{user.Username} adlı kullanıcının hesabı onaylandı.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Role != "SuperUser")
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{user.Username} adlı kullanıcı başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Süper kullanıcı silinemez.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}