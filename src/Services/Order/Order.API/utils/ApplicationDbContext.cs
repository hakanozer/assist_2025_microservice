using Microsoft.EntityFrameworkCore;
using Order.API.Models;
namespace Order.API.utils
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
            public DbSet<ProductOrder> Orders => Set<ProductOrder>();
            public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

            public DbSet<OrderSaga> OrderSagas { get; set; } // YENİ!
        
    }
}