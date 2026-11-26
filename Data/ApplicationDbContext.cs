using Microsoft.EntityFrameworkCore;
using SupertexWeb.Models;

namespace SupertexWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<HeroSection> HeroSections { get; set; }
        public DbSet<AboutPage> AboutPages { get; set; }
        public DbSet<HomeAboutSection> HomeAboutSections { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<CustomerRequest> CustomerRequests { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
    }
}   