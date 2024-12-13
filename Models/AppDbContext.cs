using webMVC.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using webMVC.Models.Contact;
using webMVC.Models.Blog;
 
namespace webMVC.Models
{
    // webMVC.Models.AppDbContext
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public DbSet<ContactModel>? Contacts { get; set; }
        public DbSet<Category>? Categories { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { 
        }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName!.StartsWith("AspNet"))
                {
                    entityType.SetTableName(tableName.Substring(6));
                }
            }
            
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Slug);
            });
        }
    }
}
