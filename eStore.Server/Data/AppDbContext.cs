using eStore.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace eStore.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(pt => pt.Order)
                .WithMany()
                .HasForeignKey(pt => pt.OrderId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
