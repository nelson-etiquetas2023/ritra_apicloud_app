using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Shared.Dtos.AppMovil;
using Shared.Security;

namespace API.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Productos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<OrderFisicoHeader> Order_InvFisico_Header { get; set; }
        public DbSet<OrderFisicoDetails> Order_InvFisico_Details { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Parameter> Parametros { get; set; }
        public DbSet<ScanProducts> ScanProducts { get; set; }
        public DbSet<ProductImage> Images { get; set; }
        public DbSet<UploadResult> Uploads { get; set; }
        public DbSet<OrderPurchase> OrderPurchase { get; set; }
        public DbSet<OrderPurchaseDetails> OrderPurchaseDetails { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //establece la precision de campo total costo.
            modelBuilder.Entity<OrderPurchase>().Property(o => o.TotalCosto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderPurchaseDetails>().Property(o => o.Costo)
                .HasPrecision(18, 2);


            // Configurar relación One-to-Many entre Product y ProductImage
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderFisicoHeader>()
                        .HasMany(o => o.OrdersDetails)
                        .WithOne(d => d.Order)
                        .HasForeignKey(d => d.OrderNumberID)
                        .HasPrincipalKey(o => o.OrderNumberID)
                        .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
