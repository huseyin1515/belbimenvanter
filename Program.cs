using BelbimEnv.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

// 1. Builder Nesnesini Olu�turma
var builder = WebApplication.CreateBuilder(args);


// 2. Servisleri Konfig�re Etme (Dependency Injection)

// Kestrel Web Sunucusu Limitlerini Yap�land�rma (HTTP 431 Hatas� ��z�m�)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Sunucunun kabul edece�i maksimum request header boyutunu 64 KB'a ��kar.
    serverOptions.Limits.MaxRequestHeadersTotalSize = 65536;
});

// Veritaban� Ba�lant�s�n� (DbContext) Servislere Ekleme
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Cookie Tabanl� Kimlik Do�rulama (Authentication) Servisini Ekleme
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


// 3. Middleware Pipeline'� Olu�turma
var app = builder.Build();

// Geli�tirme Ortam� Ayarlar�
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Detayl� hata sayfalar�n� g�ster
}
else
{
    app.UseExceptionHandler("/Home/Error"); // Genel hata yakalama sayfas�
    app.UseHsts(); // Taray�c�lar� HTTPS kullanmaya zorla
}

// Temel Middleware'ler
app.UseHttpsRedirection(); // HTTP isteklerini HTTPS'e y�nlendir
app.UseStaticFiles();      // wwwroot klas�r�ndeki statik dosyalara (CSS, JS, resimler) eri�imi sa�la
app.UseRouting();          // Gelen iste�in hangi endpoint'e gidece�ini belirle

// G�venlik Middleware'leri (S�ralama �nemli!)
app.UseAuthentication();   // "Sen kimsin?" sorusunu sor (Cookie'yi kontrol et)
app.UseAuthorization();    // "Buraya girmeye yetkin var m�?" sorusunu sor (Rolleri kontrol et)

// Varsay�lan Rota (Endpoint) Yap�land�rmas�
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Ba�lang�� sayfas�: /Account/Login


// 4. Uygulamay� �al��t�rma
app.Run();