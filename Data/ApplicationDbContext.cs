// Konum: Data/ApplicationDbContext.cs
using BelbimEnv.Models;
using Microsoft.EntityFrameworkCore;

namespace BelbimEnv.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Server> Servers { get; set; }
    }
}