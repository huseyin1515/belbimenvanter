// Konum: Program.cs
using BelbimEnv.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext'i ve veritaban� ba�lant�s�n� servislere ekle
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Uygulama ba�lang�� rotas�n� ayarla
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Servers}/{action=Index}/{id?}");

app.Run();