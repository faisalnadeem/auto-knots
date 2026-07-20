using AutoKnots.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoKnots.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<InventoryInvestment> InventoryInvestments { get; set; }
        public DbSet<InventoryCost> InventoryCosts { get; set; }
        public DbSet<VehicleListing> VehicleListings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InventoryItem>(e =>
            {
                e.Property(x => x.CostPrice).HasPrecision(18, 2);
                e.Property(x => x.SalePrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<InventoryInvestment>(e =>
            {
                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.Property(x => x.Percentage).HasPrecision(5, 2);

                e.HasOne(x => x.InventoryItem)
                    .WithMany(i => i.Investments)
                    .HasForeignKey(x => x.InventoryItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InventoryCost>(e =>
            {
                e.Property(x => x.Amount).HasPrecision(18, 2);

                e.HasOne(x => x.InventoryItem)
                    .WithMany()
                    .HasForeignKey(x => x.InventoryItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<VehicleListing>(e =>
            {
                e.Property(x => x.Price).HasPrecision(18, 2);
                e.HasIndex(x => x.Slug).IsUnique();
                e.HasIndex(x => x.InventoryItemId).IsUnique();
                e.HasIndex(x => new { x.Status, x.PublishedAt });
                e.HasIndex(x => new { x.Status, x.ExpiresAt });
                e.HasIndex(x => new { x.Status, x.Price });
                e.HasIndex(x => new { x.Status, x.Year });
                e.HasIndex(x => new { x.Status, x.Mileage });
                e.HasIndex(x => new { x.Status, x.FuelType });
                e.HasIndex(x => new { x.Status, x.Transmission });
                e.HasIndex(x => new { x.Status, x.BodyStyle });
                e.HasIndex(x => new { x.Status, x.Condition });

                e.HasOne(x => x.InventoryItem)
                    .WithOne(i => i.Listing)
                    .HasForeignKey<VehicleListing>(x => x.InventoryItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ListingImage>(e =>
            {
                e.HasIndex(x => new { x.VehicleListingId, x.SortOrder });
                e.HasOne(x => x.VehicleListing)
                    .WithMany(x => x.Images)
                    .HasForeignKey(x => x.VehicleListingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

