using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;

namespace Product.API.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<ProductEntity> Products { get; set; }
        
    }

}