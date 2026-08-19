using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Shared.Dtos.AppMovil;
using Shared.Dtos.CargasIniciales;
using Shared.Dtos.Compras;
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
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductUnit> ProductUnits { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<ScanProducts> ScanProducts { get; set; }
        public DbSet<ProductImage> Images { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Enterprise> Enterprises { get; set; }
        public DbSet<OrderPurchase> OrderPurchase { get; set; }
        public DbSet<OrderPurchaseDetails> OrderPurchaseDetails { get; set; }
        public DbSet<OrdenCompra> Compra { get; set; }
        public DbSet<DetalleCompras> DetalleCompra { get; set; }
        public DbSet<Inicial> CargasIniciales { get; set; }
        public DbSet<DetalleInicial> CargasInicialesDetalles { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //establece la precision de campo total costo.
            modelBuilder.Entity<OrderPurchase>().Property(o => o.TotalCosto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderPurchaseDetails>().Property(o => o.Costo)
                .HasPrecision(18, 2);


            modelBuilder.Entity<DetalleCompras>().Property(o => o.Costo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DetalleCompras>().Property(o => o.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>().Property(p => p.Costo)
                .HasPrecision(18, 2);

            // Índice único para Product_Code
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Product_Code)
                .IsUnique();

            // Índice único para SupplierCode (P######)
            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.SupplierCode)
                .IsUnique();

            // Índice único para CustomerCode (C######)
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerCode)
                .IsUnique();


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

            modelBuilder.Entity<OrdenCompra>()
                .HasMany(o => o.Items)
                .WithOne(o => o.Order)
                .HasForeignKey(d => d.Numero)
                .HasPrincipalKey(o => o.Numero)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inicial>()
                .HasMany(i => i.Detalles)
                .WithOne(d => d.Inicial)
                .HasForeignKey(d => d.InicialId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetalleInicial>()
                .Property(d => d.Costo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Location>()
                .Property(l => l.Capacity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Location>()
                .Property(l => l.CurrentCapacity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DetalleInicial>()
                .HasIndex(d => d.InicialId);


        }
    }
}
