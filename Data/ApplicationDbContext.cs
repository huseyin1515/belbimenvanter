using BelbimEnv.Models;
using Microsoft.EntityFrameworkCore;

namespace BelbimEnv.Data
{
    // Kalıtım tekrar DbContext'e çevrildi
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Server> Servers { get; set; }
        public DbSet<PortDetay> PortDetaylari { get; set; }
        public DbSet<User> Users { get; set; } // YENİ EKLENDİ
        public DbSet<VirtualMachine> VirtualMachines { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Bu satır kalabilir, zararı yok.

            builder.Entity<PortDetay>()
                .HasOne(p => p.Server)
                .WithMany(s => s.PortDetaylari)
                .HasForeignKey(p => p.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}