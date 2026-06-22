using API.Data;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Inventory
{
    public class InventoryService(ApplicationDbContext context) : IInventoryService
    {
        public ApplicationDbContext Context { get; set; } = context;

        public async Task<bool> SaveNumberConsecInventoryAsync(string numero,string filter)
        {
            var setting = Context.Parametros.Where(x => x.Module == filter);
            if (setting == null) return  false;
            setting.FirstOrDefault()!.Value1 = numero;
            Context.SaveChanges();
            return true;
        }
        public async Task<DocumentSettings> GetConfigById(string filter)
        {
            var setting =  Context.Parametros.Where(x => x.Module == filter);
            
            if (setting == null) return new DocumentSettings();

            DocumentSettings ds = new()
            {
                Consec = Convert.ToInt32(setting.FirstOrDefault()!.Value1),
                Prefijo = setting.FirstOrDefault()!.Value2,
                useSeparator = Convert.ToBoolean(setting.FirstOrDefault()!.Value3),
                usePref = Convert.ToBoolean(setting.FirstOrDefault()!.Value4),
                CharacterSeparator = setting.FirstOrDefault()!.Value5
            };
            return ds;
        }
        public async Task<bool> UpdateScanProductsAsync(ScanProducts scanprducts)
        {
            if (scanprducts == null) return false;  

            var existing = await Context.ScanProducts.FindAsync(scanprducts.guid);
            if (existing == null) return false;

            existing.Quantity = scanprducts.Quantity;
            existing.Ubicacion = scanprducts.Ubicacion; 
            existing.Estado = scanprducts.Estado;
            existing.Unidad = scanprducts.Unidad;
            existing.Category = scanprducts.Category;
            existing.StateData = "Updated";
            existing.DateScan = scanprducts.DateScan;

            Context.SaveChanges();
            return true; 

        }
        public async Task<bool> DeleteScanProductsAsync(Guid id)
        {
            var scanProducts = await Context.ScanProducts.FindAsync(id);

            if (scanProducts == null) return false;
            Context.ScanProducts.Remove(scanProducts);
            await Context.SaveChangesAsync();

            return true;
        }
        public async Task<List<ScanProducts>> GetScanProductsAsync(string OrderId) 
        {
            var productsScan = await Context.ScanProducts
                .Where(p => p.OrdenId == OrderId).ToListAsync();

            return productsScan;
        }
        public async Task<bool> SaveDataProductScanAsync(List<ScanProducts> products)
        {
            if (products == null) return false;

            foreach (var product in products) 
            {
                product.StateData = "Saved";
                Context.ScanProducts.Add(product);
            }

            Context.SaveChanges();

            return true;
        }

        [EnableCors]
        [HttpGet]
        public async Task<IEnumerable<OrderFisicoHeader>> GetOrdersAsync()
        {
            var orders = await Context.Order_InvFisico_Header
                .Include(o => o.OrdersDetails).ToListAsync();            

            return orders;
        }
        public async Task<OrderFisicoHeader?> GetOrderByIdAsync(string OrderNumber)
        {
            return await Context.Order_InvFisico_Header.Include(o => o.OrdersDetails)
                .FirstOrDefaultAsync(o => o.OrderNumberID == OrderNumber);
        }
        public async Task<OrderFisicoHeader> CreateOrderAsync(OrderFisicoHeader order)
        {
            Context.Order_InvFisico_Header.Add(order);
            await Context.SaveChangesAsync();
            return order;
        }
        public async Task<OrderFisicoHeader?> UpdateOrderAsync(string OrderNumber, OrderFisicoHeader order)
        {
            //busco la orden a modificar en la base de datos.
            var existing = await Context.Order_InvFisico_Header.Include(o => o.OrdersDetails)
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
            Context.Order_InvFisico_Details.RemoveRange(existing.OrdersDetails);


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
            await Context.Order_InvFisico_Details.AddRangeAsync(newItems);
            await Context.SaveChangesAsync();
            return existing;
        }
        public async Task<bool> DeleteOrderAsync(string id)
        {
            var order = await Context.Order_InvFisico_Header.FindAsync(id);

            if (order == null) return false;
            Context.Order_InvFisico_Header.Remove(order);
            await Context.SaveChangesAsync();

            return true;

        }
    }
}
