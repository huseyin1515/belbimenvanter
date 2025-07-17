using BelbimEnv.Models;
using Microsoft.EntityFrameworkCore;

namespace BelbimEnv.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Server> Servers { get; set; }
        public DbSet<PortDetay> PortDetaylari { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PortDetay>()
                .HasOne(p => p.Server)
                .WithMany(s => s.PortDetaylari)
                .HasForeignKey(p => p.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}