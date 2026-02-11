using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
        public DbSet<Product> Productos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<OrderFisicoHeader> Order_InvFisico_Header { get; set; }
        public DbSet<OrderFisicoDetails> Order_InvFisico_Details { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Parametro> Parametros { get; set; }
        public DbSet<ScanProducts> scanProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderFisicoHeader>()
                        .HasMany(o => o.OrdersDetails)
                        .WithOne(d => d.Order)
                        .HasForeignKey(d => d.OrderNumberID)
                        .HasPrincipalKey(o => o.OrderNumberID)
                        .OnDelete(DeleteBehavior.Cascade) ;
        }
    }
}
