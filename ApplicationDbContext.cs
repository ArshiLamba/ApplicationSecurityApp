using ApplicationSecurityApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApplicationSecurityApp
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Member> Members { get; set; } // ✅ Correct
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.Email)
                .IsUnique(); // Ensure email uniqueness
        }
    }
}
