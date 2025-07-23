using BelbimEnv.Data;
using Microsoft.AspNetCore.Authentication.Cookies; // Gerekli using eklendi
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// DbContext'i ve veritabaný baðlantýsýný servislere ekle
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ==========================================================
// === YENÝ MANUEL AUTHENTICATION SÝSTEMÝ ===
// ==========================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giriþ yapýlmamýþsa yönlendirilecek sayfa
        options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisi yoksa yönlendirilecek sayfa
        options.ExpireTimeSpan = System.TimeSpan.FromMinutes(30); // Oturum süresi
        options.SlidingExpiration = true; // Her istekte süreyi uzat
    });
// ==========================================================

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// YENÝDEN EKLENDÝ VE AKTÝF EDÝLDÝ
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Baþlangýç sayfasý artýk Login

app.Run();