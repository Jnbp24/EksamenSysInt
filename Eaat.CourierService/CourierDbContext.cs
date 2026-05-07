using Eaat.Database;
using Microsoft.EntityFrameworkCore;

namespace Eaat.CourierService
{
    public class CourierDbContext : DbContext
    {
        public CourierDbContext(DbContextOptions<CourierDbContext> options) : base(options) {  }
        public DbSet<OrderClaim> OrderClaims => Set<OrderClaim>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderClaim>()
                .HasKey(x => x.Id); // NEW PK

            modelBuilder.Entity<OrderClaim>()
                .HasIndex(x => x.OrderId)
                .IsUnique();

            modelBuilder.Entity<OrderClaim>()
                .Property(x => x.CourierName)
                .IsRequired();
        }
    }
}