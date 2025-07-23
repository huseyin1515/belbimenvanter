using BelbimEnv.Data;
using BelbimEnv.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // DateTime için eklendi
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BelbimEnv.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && user.Role == "Bekleyen")
                {
                    ModelState.AddModelError(string.Empty, "Hesabınız henüz bir yönetici tarafından onaylanmamıştır.");
                    return View(model);
                }

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role)
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties { IsPersistent = model.RememberMe });

                    return RedirectToLocal(returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya parola.");
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        
        [Authorize] // Sadece giriş yapmış kullanıcılar erişebilir
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var model = new ManageProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            // Email'in başkası tarafından kullanılıp kullanılmadığını kontrol et
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != user.Id);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Bu email adresi başka bir kullanıcı tarafından kullanılıyor.");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;

            // Eğer yeni bir parola girildiyse, güncelle
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";
            return RedirectToAction("Manage");
        }
        // GÜNCELLENMİŞ REGISTER METODU
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten alınmış.");
                    return View(model);
                }
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Bu email adresi zaten kullanılıyor.");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    // YENİ ALANLARI ATA
                    FullName = model.FullName,
                    Email = model.Email,
                    CreatedAt = DateTime.Now
                };

                bool isFirstUser = !await _context.Users.AnyAsync();

                if (isFirstUser)
                {
                    user.Role = "SuperUser";
                }
                else
                {
                    user.Role = "Bekleyen";
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (isFirstUser)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role)
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    TempData["SuccessMessage"] = "Süper kullanıcı hesabı başarıyla oluşturuldu ve giriş yapıldı.";
                    return RedirectToAction("Index", "Servers");
                }
                else
                {
                    return View("RegisterSuccess");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl)) { return Redirect(returnUrl); }
            else { return RedirectToAction("Index", "Servers"); }
        }
    }
}