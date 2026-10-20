using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data.Entities;
using ScanProMovil.Entities;

namespace ScanProMovil.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetails> OrderItems => Set<OrderDetails>();
        public DbSet<OrdenCompra> PurchaseOrders => Set<OrdenCompra>();
        public DbSet<DetalleCompra> PurchaseOrderItems => Set<DetalleCompra>();
        public DbSet<ProductImagen> ProductImages => Set<ProductImagen>();
        public DbSet<User> Users => Set<User>();
        public DbSet<ConsecutivoCompra> Consecutivos => Set<ConsecutivoCompra>();
        public DbSet<ConsecutivoStockInit> ConsecutivosStockInit => Set<ConsecutivoStockInit>();
        public DbSet<StockInit> StockInits => Set<StockInit>();
        public DbSet<StockItem> StockItems => Set<StockItem>();
        public DbSet<PickingOrder> PickingOrders => Set<PickingOrder>();
        public DbSet<PickingOrderDetail> PickingOrderItems => Set<PickingOrderDetail>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(e =>
            {
                e.ToTable("Products");
                e.HasKey(p => p.Product_Id);
                e.HasIndex(p => p.product_code).IsUnique();
                e.HasIndex(p => p.CodeBar).IsUnique().HasFilter("CodeBar IS NOT NULL AND CodeBar <> ''");
                e.Property(p => p.Price).HasConversion<double>();
                e.HasMany(p => p.Images)
                    .WithOne()
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Order>(e =>
            {
                e.ToTable("Orders");
                e.HasKey(o => o.OrderId);
                e.HasIndex(o => o.OrderNumber);
                e.HasMany(o => o.Items)
                    .WithOne()
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderDetails>(e =>
            {
                e.ToTable("OrderItems");
                e.HasKey(i => i.DetailId);
            });

            modelBuilder.Entity<OrdenCompra>(e =>
            {
                e.ToTable("PurchaseOrders");
                e.HasKey(o => o.Numero);
                e.HasIndex(o => o.OrderId);
                e.HasMany(o => o.Items)
                    .WithOne()
                    .HasForeignKey(i => i.Numero)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DetalleCompra>(e =>
            {
                e.ToTable("PurchaseOrderItems");
                e.HasKey(i => i.DetailId);
            });

            modelBuilder.Entity<ProductImagen>(e =>
            {
                e.ToTable("ProductImages");
                e.HasKey(i => i.Id);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(u => u.UserId);
            });

            modelBuilder.Entity<ConsecutivoCompra>(e =>
            {
                e.ToTable("ConsecutivosCompra");
                e.HasKey(c => c.Tipo_Documento);
            });

            modelBuilder.Entity<ConsecutivoStockInit>(e =>
            {
                e.ToTable("ConsecutivosStockInit");
                e.HasKey(c => c.Tipo_Documento);
            });

            modelBuilder.Entity<StockInit>(e =>
            {
                e.ToTable("StockInits");
                e.HasKey(s => s.Numero);
                e.HasMany(s => s.Items)
                    .WithOne()
                    .HasForeignKey(i => i.Numero)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockItem>(e =>
            {
                e.ToTable("StockItems");
                e.HasKey(i => i.Id);
            });

            modelBuilder.Entity<PickingOrder>(e =>
            {
                e.ToTable("PickingOrders");
                e.HasKey(p => p.PickingId);
                e.HasIndex(p => p.NumeroPedidoVenta);
                e.HasIndex(p => p.Estado);
                e.HasMany(p => p.Items)
                    .WithOne()
                    .HasForeignKey(i => i.PickingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PickingOrderDetail>(e =>
            {
                e.ToTable("PickingOrderItems");
                e.HasKey(i => i.DetailId);
            });
        }
    }
}
