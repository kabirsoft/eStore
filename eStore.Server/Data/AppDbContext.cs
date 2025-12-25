using eStore.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace eStore.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Product> Products => Set<Product>();
    }
}
