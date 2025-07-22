using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Controllers;
using RRealEstateApi.Models;

namespace RRealEstateApi.Data
{
    public class RealEstateDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, String>
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
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<LoginActivity> LoginActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  Cascade delete: When a user is deleted, their messages go too
            modelBuilder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Property)
                .WithMany()
                .HasForeignKey(m => m.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            //  Cascade delete for watchlist
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

            //  Cascade delete for transaction if user is deleted
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Buyer)
                .WithMany()
                .HasForeignKey(t => t.BuyerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Property)
                .WithMany()
                .HasForeignKey(t => t.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            //  Cascade delete for notifications
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: Unique constraint for watchlist
            modelBuilder.Entity<WatchlistItem>()
                .HasIndex(w => new { w.UserId, w.PropertyId })
                .IsUnique();

            // Cascade for Listing to Property
            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Property)
                .WithMany()
                .HasForeignKey(l => l.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Agent)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.AgentId)
                .OnDelete(DeleteBehavior.SetNull); // or .Cascade if you want agent deletion to delete users

        }
    }
}