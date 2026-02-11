using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Inventory
{
    public class InventoryService : IInventoryService
    {
        public ApplicationDbContext context { get; set; }
        public InventoryService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<bool> DeleteScanProductsAsync(Guid id)
        {
            var scanProducts = await context.scanProducts.FindAsync(id);

            if (scanProducts == null) return false;
            context.scanProducts.Remove(scanProducts);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ScanProducts>> GetScanProductsAsync(string OrderId) 
        {
            var productsScan = await context.scanProducts
                .Where(p => p.OrdenId == OrderId).ToListAsync();

            return productsScan;
        }

        public async Task<bool> SaveDataProductScanAsync(List<ScanProducts> products)
        {
            if (products == null) return false;

            foreach (var product in products) 
            {
                product.StateData = "Saved";
                context.scanProducts.Add(product);
            }

            context.SaveChanges();

            return true;
        }


        public async Task<IEnumerable<OrderFisicoHeader>> GetOrdersAsync()
        {
            var orders = await context.Order_InvFisico_Header
                .Include(o => o.OrdersDetails).ToListAsync();            


            return orders;
        }

        public async Task<OrderFisicoHeader?> GetOrderByIdAsync(string OrderNumber)
        {
            return await context.Order_InvFisico_Header.Include(o => o.OrdersDetails)
                .FirstOrDefaultAsync(o => o.OrderNumberID == OrderNumber);
        }

        public async Task<OrderFisicoHeader> CreateOrderAsync(OrderFisicoHeader order)
        {
            context.Order_InvFisico_Header.Add(order);
            await context.SaveChangesAsync();
            return order;
        }

        public async Task<OrderFisicoHeader?> UpdateOrderAsync(string OrderNumber, OrderFisicoHeader order)
        {
            //busco la orden a modificar en la base de datos.
            var existing = await context.Order_InvFisico_Header.Include(o => o.OrdersDetails)
                .FirstOrDefaultAsync(o => o.OrderNumberID == OrderNumber);

            //valido.
            if (existing == null) return null;

            //datos del header.
            existing.Order_Date = order.Order_Date;
            existing.Order_Hour = order.Order_Hour;
            existing.Notes = order.Notes;
            existing.Items = order.Items;
            existing.Status = order.Status;
            existing.Area_Almacen = order.Area_Almacen;
            existing.Person_Create = order.Person_Create;
            existing.Sincro_Document = order.Sincro_Document;
            existing.Status_Name = order.Status_Name;

            //borra todos los items del detalle para agregar los editados.
            context.Order_InvFisico_Details.RemoveRange(existing.OrdersDetails);


            //insertar los nuevos items a actualizar.
            var newItems = new List<OrderFisicoDetails>();
            int fila = 1;

            foreach (var detail in order.OrdersDetails)
            {
                var newItem = new OrderFisicoDetails
                {
                    Id = Guid.NewGuid(),
                    OrderNumberID = existing.OrderNumberID,
                    Renglon_Id = fila,
                    Product_id = detail.Product_id,
                    Product_name = detail.Product_name,
                    Product_Type = detail.Product_Type,
                    Roll_Id = detail.Roll_Id,
                    Width_Fisico = detail.Width_Fisico,
                    Length_Fisico = detail.Length_Fisico,
                    Width_Sistema = detail.Width_Sistema,
                    Length_Sistema = detail.Length_Sistema,
                    Width_Dif = detail.Width_Dif,
                    Length_Dif = detail.Length_Dif,
                    Code_Unique = detail.Code_Unique,
                    Ubicacion = detail.Ubicacion,
                    Notes = detail.Notes,
                    Product_Estado = detail.Product_Estado

                };
                newItems.Add(newItem);
                fila += 1;
            }
            await context.Order_InvFisico_Details.AddRangeAsync(newItems);
            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteOrderAsync(string id)
        {
            var order = await context.Order_InvFisico_Header.FindAsync(id);

            if (order == null) return false;
            context.Order_InvFisico_Header.Remove(order);
            await context.SaveChangesAsync();

            return true;

        }

       
    }
}
