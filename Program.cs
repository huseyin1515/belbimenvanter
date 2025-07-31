using BelbimEnv.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

// 1. Builder Nesnesini Oluþturma
var builder = WebApplication.CreateBuilder(args);


// 2. Servisleri Konfigüre Etme (Dependency Injection)

// Kestrel Web Sunucusu Limitlerini Yapýlandýrma (HTTP 431 Hatasý Çözümü)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Sunucunun kabul edeceði maksimum request header boyutunu 64 KB'a çýkar.
    serverOptions.Limits.MaxRequestHeadersTotalSize = 65536;
});

// Veritabaný Baðlantýsýný (DbContext) Servislere Ekleme
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Cookie Tabanlý Kimlik Doðrulama (Authentication) Servisini Ekleme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = System.TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// MVC Servislerini (Controllers ve Views) Ekleme
builder.Services.AddControllersWithViews();


// 3. Middleware Pipeline'ý Oluþturma
var app = builder.Build();

// Geliþtirme Ortamý Ayarlarý
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Detaylý hata sayfalarýný göster
}
else
{
    app.UseExceptionHandler("/Home/Error"); // Genel hata yakalama sayfasý
    app.UseHsts(); // Tarayýcýlarý HTTPS kullanmaya zorla
}

// Temel Middleware'ler
app.UseHttpsRedirection(); // HTTP isteklerini HTTPS'e yönlendir
app.UseStaticFiles();      // wwwroot klasöründeki statik dosyalara (CSS, JS, resimler) eriþimi saðla
app.UseRouting();          // Gelen isteðin hangi endpoint'e gideceðini belirle

// Güvenlik Middleware'leri (Sýralama Önemli!)
app.UseAuthentication();   // "Sen kimsin?" sorusunu sor (Cookie'yi kontrol et)
app.UseAuthorization();    // "Buraya girmeye yetkin var mý?" sorusunu sor (Rolleri kontrol et)

// Varsayýlan Rota (Endpoint) Yapýlandýrmasý
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Baþlangýç sayfasý: /Account/Login


// 4. Uygulamayý Çalýþtýrma
app.Run();