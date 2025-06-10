using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Controllers;
using RRealEstateApi.Models;

namespace RRealEstateApi.Data
{
    public class RealEstateDbContext : IdentityDbContext<ApplicationUser>
    {
        public RealEstateDbContext(DbContextOptions<RealEstateDbContext> options)
            : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<WatchlistItem> WatchlistItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // ✅ Correct declaration
        public DbSet<PropertyImage> PropertyImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Property)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WatchlistItem>(entity =>
                entity.HasIndex(w => new { w.UserId, w.PropertyId }).IsUnique());

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Property)
                .WithMany()
                .HasForeignKey(l => l.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WatchlistItem>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WatchlistItem>()
                .HasOne(w => w.Property)
                .WithMany()
                .HasForeignKey(w => w.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}