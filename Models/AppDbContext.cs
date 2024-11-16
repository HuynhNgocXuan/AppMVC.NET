using Microsoft.EntityFrameworkCore;
using webMVC.Models.Contact;

namespace webMVC.Models 
{
    // App.Models.AppDbContext
    public class AppDbContext : DbContext
    {
        public DbSet<ContactModel> Contacts{ get; set; }
        
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
            
            // foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            // {
            //     var tableName = entityType.GetTableName();
            //     if (tableName.StartsWith("AspNet"))
            //     {
            //         entityType.SetTableName(tableName.Substring(6));
            //     }
            // }

        }
    }
}
