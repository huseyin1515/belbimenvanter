using BelbimEnv.Data;
using Microsoft.AspNetCore.Authentication.Cookies; // Gerekli using eklendi
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// DbContext'i ve veritaban� ba�lant�s�n� servislere ekle
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ==========================================================
// === YEN� MANUEL AUTHENTICATION S�STEM� ===
// ==========================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giri� yap�lmam��sa y�nlendirilecek sayfa
        options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisi yoksa y�nlendirilecek sayfa
        options.ExpireTimeSpan = System.TimeSpan.FromMinutes(30); // Oturum s�resi
        options.SlidingExpiration = true; // Her istekte s�reyi uzat
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

// YEN�DEN EKLEND� VE AKT�F ED�LD�
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Ba�lang�� sayfas� art�k Login

app.Run();