using KMC.EventPlatformAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KMC.EventPlatformAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<EventRegistration> EventRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(256);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.NormalizedUserName)
                .IsUnique();

            // Event configuration
            modelBuilder.Entity<Event>()
                .HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // EventRegistration configuration
            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(er => er.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(er => er.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EventRegistration>()
                .HasIndex(er => new { er.EventId, er.UserId })
                .IsUnique();
        }
    }
}