using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public required DbSet<Client> Clientes { get; set; }
        public required DbSet<Employe> Usuarios { get; set; }
        public required DbSet<Product> Productos { get; set; }
    }
}
