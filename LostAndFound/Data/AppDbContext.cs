using LostAndFound.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Item> Items => Set<Item>();

        public DbSet<ItemPhoto> ItemPhotos => Set<ItemPhoto>();

        public DbSet<Report> Reports => Set<Report>();

        public DbSet<ItemClaim> ItemClaims => Set<ItemClaim>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Keep ASP.NET Identity, but use the table name required by the project.
            modelBuilder.Entity<ApplicationUser>()
                .ToTable("Users");

            modelBuilder.Entity<Item>()
                .HasOne(item => item.Category)
                .WithMany()
                .HasForeignKey(item => item.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemPhoto>()
                .HasOne(photo => photo.Item)
                .WithMany(item => item.Photos)
                .HasForeignKey(photo => photo.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Report>()
                .HasOne(report => report.Item)
                .WithMany(item => item.Reports)
                .HasForeignKey(report => report.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Report>()
                .HasOne(report => report.CreatedBy)
                .WithMany(user => user.Reports)
                .HasForeignKey(report => report.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemClaim>()
                .HasOne(claim => claim.Report)
                .WithMany(report => report.Claims)
                .HasForeignKey(claim => claim.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemClaim>()
                .HasOne(claim => claim.Claimant)
                .WithMany(user => user.ItemClaims)
                .HasForeignKey(claim => claim.ClaimantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
