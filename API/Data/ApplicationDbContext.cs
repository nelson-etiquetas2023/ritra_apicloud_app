using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Shared.Dtos.AppMovil;
using Shared.Dtos.Inventarios;
using Shared.Dtos.Inventario;
using Shared.Dtos.Compras;
using Shared.Dtos.Turnos;
using Shared.Dtos.Ventas;
using Shared.Dtos.Picking;
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
        public DbSet<InventarioTeorico> InventarioTeorico { get; set; }
        public DbSet<InventarioTeoricoDetalle> InventarioTeoricoDetalles { get; set; }
        public DbSet<InventarioFisico> InventarioFisico { get; set; }
        public DbSet<InventarioFisicoDetalle> InventarioFisicoDetalles { get; set; }
        public DbSet<PedidoVenta> PedidoVenta { get; set; }
        public DbSet<DetalleVenta> PedidoVentaDetalles { get; set; }
        public DbSet<VentaPago> VentaPagos { get; set; }
        public DbSet<AjusteInventario> AjustesInventario { get; set; }
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<Vendedor> Vendedores { get; set; }
        public DbSet<LabelTemplate> LabelTemplates { get; set; }
        public DbSet<Printer> Printers { get; set; }
        public DbSet<PrintJob> PrintJobs { get; set; }
        public DbSet<TurnoCaja> TurnosCaja { get; set; }
        public DbSet<TurnoDenominacion> TurnoDenominaciones { get; set; }
        public DbSet<TurnoMovimiento> TurnoMovimientos { get; set; }

        public DbSet<PickingOrder> PickingOrders { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(user => user.Nickname)
                .HasMaxLength(50);

            modelBuilder.Entity<User>()
                .HasIndex(user => user.Nickname)
                .IsUnique()
                .HasFilter("[Nickname] <> ''");

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

            // Índice único filtrado para Codebar (evita duplicados, permite vacíos)
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Codebar)
                .IsUnique()
                .HasFilter("[Codebar] IS NOT NULL AND [Codebar] <> ''");

            // Índice único para SupplierCode (P######)
            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.SupplierCode)
                .IsUnique();

            // Índice único para CustomerCode (C######)
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerCode)
                .IsUnique();

            // Índice único para almacen_code (ALM-####)
            modelBuilder.Entity<Almacen>()
                .HasIndex(a => a.almacen_code)
                .IsUnique();

            // Etiquetas: códigos únicos de plantillas e impresoras
            modelBuilder.Entity<LabelTemplate>()
                .HasIndex(t => t.Codigo)
                .IsUnique();

            modelBuilder.Entity<Printer>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<PrintJob>()
                .HasIndex(j => j.Status);

            // Índice único para vendedor_code (VEN-####)
            modelBuilder.Entity<Vendedor>()
                .HasIndex(v => v.vendedor_code)
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

            modelBuilder.Entity<InventarioTeorico>()
                .HasMany(i => i.Detalles)
                .WithOne(d => d.InventarioTeorico)
                .HasForeignKey(d => d.InventarioTeoricoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventarioTeorico>()
                .HasIndex(i => i.Numero)
                .IsUnique();

            modelBuilder.Entity<InventarioTeoricoDetalle>()
                .Property(d => d.Costo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<InventarioFisico>()
                .HasMany(d => d.Detalles)
                .WithOne(dd => dd.InventarioFisico)
                .HasForeignKey(dd => dd.InventarioFisicoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventarioFisico>()
                .HasIndex(d => d.Numero)
                .IsUnique();

            modelBuilder.Entity<InventarioFisicoDetalle>()
                .Property(d => d.Costo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<InventarioFisicoDetalle>()
                .HasIndex(d => d.InventarioFisicoId);

            modelBuilder.Entity<Location>()
                .Property(l => l.Capacity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Location>()
                .Property(l => l.CurrentCapacity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<InventarioTeoricoDetalle>()
                .HasIndex(d => d.InventarioTeoricoId);

            modelBuilder.Entity<PedidoVenta>()
                .HasMany(p => p.Items)
                .WithOne(d => d.PedidoVenta)
                .HasForeignKey(d => d.PedidoVentaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PedidoVenta>()
                .HasMany(p => p.Pagos)
                .WithOne(g => g.PedidoVenta)
                .HasForeignKey(g => g.PedidoVentaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VentaPago>()
                .Property(g => g.Monto).HasPrecision(18, 2);

            modelBuilder.Entity<VentaPago>()
                .HasIndex(g => g.PedidoVentaId);

            modelBuilder.Entity<PedidoVenta>()
                .Property(p => p.Subtotal).HasPrecision(18, 2);

            modelBuilder.Entity<PedidoVenta>()
                .Property(p => p.Descuento).HasPrecision(18, 2);

            modelBuilder.Entity<PedidoVenta>()
                .Property(p => p.Impuesto).HasPrecision(18, 2);

            modelBuilder.Entity<PedidoVenta>()
                .Property(p => p.Total).HasPrecision(18, 2);

            modelBuilder.Entity<DetalleVenta>()
                .Property(d => d.Precio).HasPrecision(18, 2);

            modelBuilder.Entity<AjusteInventario>()
                .HasIndex(a => a.Lote);

            modelBuilder.Entity<AjusteInventario>()
                .HasIndex(a => a.ProductoId);

            modelBuilder.Entity<DetalleVenta>()
                .Property(d => d.Descuento).HasPrecision(18, 2);

            modelBuilder.Entity<DetalleVenta>()
                .Property(d => d.Stock).HasPrecision(18, 2);

            modelBuilder.Entity<DetalleVenta>()
                .HasIndex(d => d.PedidoVentaId);

            modelBuilder.Entity<TurnoCaja>()
                .Property(t => t.FondoInicial).HasPrecision(18, 2);

            modelBuilder.Entity<TurnoCaja>()
                .Property(t => t.Esperado).HasPrecision(18, 2);

            modelBuilder.Entity<TurnoCaja>()
                .Property(t => t.Contado).HasPrecision(18, 2);

            modelBuilder.Entity<TurnoCaja>()
                .Property(t => t.Diferencia).HasPrecision(18, 2);

            modelBuilder.Entity<TurnoCaja>()
                .HasIndex(t => new { t.Caja, t.Status });

            modelBuilder.Entity<TurnoDenominacion>()
                .Property(d => d.Denominacion).HasPrecision(18, 2);

            modelBuilder.Entity<TurnoDenominacion>()
                .HasIndex(d => d.TurnoId);

            modelBuilder.Entity<TurnoMovimiento>()
                .Property(m => m.Monto).HasPrecision(18, 2);

            modelBuilder.Entity<TurnoMovimiento>()
                .HasIndex(m => m.TurnoId);

            modelBuilder.Entity<PedidoVenta>()
                .HasIndex(p => p.TurnoId);

            modelBuilder.Entity<PickingOrder>(e =>
            {
                e.ToTable("PickingOrders");
                e.HasKey(p => p.Id);
                e.HasIndex(p => p.NumeroPedidoVenta);
                e.HasIndex(p => p.Estado);
            });


        }
    }
}
