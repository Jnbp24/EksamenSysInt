using Microsoft.EntityFrameworkCore;

namespace Eaat.Database
{
    public class EaatDbContext : DbContext
    {
        public EaatDbContext(DbContextOptions<EaatDbContext> options) : base(options) { }

        public DbSet<OrderClaim> OrderClaims => Set<OrderClaim>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderClaim>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<OrderClaim>()
                .HasIndex(x => x.OrderId)
                .IsUnique();

            modelBuilder.Entity<OrderClaim>()
                .Property(x => x.CourierName)
                .IsRequired();
        }
    }
}