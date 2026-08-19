using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Config
{
    public class ConfigService : IConfigService
    {
        readonly ApplicationDbContext context;

        public ConfigService(ApplicationDbContext context)
        {
            this.context = context; 
        }

        private async Task EnsureCategoriesTableAsync()
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[Categories]', N'U') IS NULL
BEGIN
    CREATE TABLE [Categories]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Categories] PRIMARY KEY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(250) NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT 1
    );
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories]([Name]);
END");
        }

        private async Task EnsureProductUnitsTableAsync()
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[ProductUnits]', N'U') IS NULL
BEGIN
    CREATE TABLE [ProductUnits]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProductUnits] PRIMARY KEY,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(150) NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_ProductUnits_IsActive] DEFAULT 1
    );
    CREATE UNIQUE INDEX [IX_ProductUnits_Name] ON [ProductUnits]([Name]);
END");
        }
        public async Task<List<Parameter>> LoadConfigurationAsync()
        {
            var data = await context.Parametros.ToListAsync();
            return data;

        }

        public async Task<DocumentSettings?> UpdateDocumntSetting(string filter, DocumentSettings setting)
        {
            if (setting == null) return null;
            if (string.IsNullOrWhiteSpace(filter)) return null;

            var parameter = await context.Parametros.FirstOrDefaultAsync(x => x.Module == filter);
            if (parameter == null) return null;

            parameter.Value1 = setting.Consec.ToString();
            parameter.Value2 = setting.Prefijo.ToString();
            parameter.Value3 = setting.useSeparator.ToString();
            parameter.Value4 = setting.usePref.ToString();
            parameter.Value5 = setting.CharacterSeparator.ToString();

            await context.SaveChangesAsync();
            return setting;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            await EnsureCategoriesTableAsync();
            return await context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> CreateCategoryAsync(Category category)
        {
            await EnsureCategoriesTableAsync();
            if (category == null || string.IsNullOrWhiteSpace(category.Name)) return null;

            var name = category.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (await context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
                return null;

            category.Name = name;
            category.Description = category.Description?.Trim() ?? string.Empty;
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return category;
        }

        public async Task<Category?> UpdateCategoryAsync(int id, Category category)
        {
            await EnsureCategoriesTableAsync();
            if (category == null || string.IsNullOrWhiteSpace(category.Name)) return null;

            var existing = await context.Categories.FindAsync(id);
            if (existing == null) return null;

            var name = category.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (await context.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower()))
                return null;

            existing.Name = name;
            existing.Description = category.Description?.Trim() ?? string.Empty;
            existing.IsActive = category.IsActive;
            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            await EnsureCategoriesTableAsync();
            var category = await context.Categories.FindAsync(id);
            if (category == null) return false;

            context.Categories.Remove(category);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ProductUnit>> GetProductUnitsAsync()
        {
            await EnsureProductUnitsTableAsync();
            return await context.ProductUnits.OrderBy(u => u.Name).ToListAsync();
        }

        public async Task<ProductUnit?> CreateProductUnitAsync(ProductUnit unit)
        {
            await EnsureProductUnitsTableAsync();
            if (unit == null || string.IsNullOrWhiteSpace(unit.Name)) return null;

            var name = unit.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (await context.ProductUnits.AnyAsync(u => u.Name.ToLower() == name.ToLower())) 
                return null;

            unit.Name = name;
            unit.Description = unit.Description?.Trim() ?? string.Empty;
            context.ProductUnits.Add(unit);
            await context.SaveChangesAsync();
            return unit;
        }

        public async Task<ProductUnit?> UpdateProductUnitAsync(int id, ProductUnit unit)
        {
            await EnsureProductUnitsTableAsync();
            if (unit == null || string.IsNullOrWhiteSpace(unit.Name)) return null;

            var existing = await context.ProductUnits.FindAsync(id);
            if (existing == null) return null;

            var name = unit.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (await context.ProductUnits.AnyAsync(u => u.Id != id && u.Name.ToLower() == name.ToLower())) 
                return null;

            existing.Name = name;
            existing.Description = unit.Description?.Trim() ?? string.Empty;
            existing.IsActive = unit.IsActive;
            await context.SaveChangesAsync();
            return existing;
        }

        private async Task EnsureWarehousesTableAsync()
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[Warehouses]', N'U') IS NULL
BEGIN
    CREATE TABLE [Warehouses]
    (
        [WarehouseId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouses] PRIMARY KEY,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_Warehouses_IsActive] DEFAULT 1
    );
    CREATE UNIQUE INDEX [IX_Warehouses_Name] ON [Warehouses]([Name]);
END");
        }

        private async Task EnsureLocationsTableAsync()
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[Locations]', N'U') IS NULL
BEGIN
    CREATE TABLE [Locations]
    (
        [LocationId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Locations] PRIMARY KEY,
        [WarehouseId] int NOT NULL CONSTRAINT [FK_Locations_Warehouses] REFERENCES [Warehouses]([WarehouseId]),
        [Code] nvarchar(30) NOT NULL,
        [Barcode] nvarchar(50) NOT NULL CONSTRAINT [DF_Locations_Barcode] DEFAULT '',
        [BarcodeType] nvarchar(20) NOT NULL CONSTRAINT [DF_Locations_BarcodeType] DEFAULT '',
        [Zone] nvarchar(20) NOT NULL CONSTRAINT [DF_Locations_Zone] DEFAULT '',
        [Aisle] nvarchar(10) NOT NULL CONSTRAINT [DF_Locations_Aisle] DEFAULT '',
        [Rack] nvarchar(10) NOT NULL CONSTRAINT [DF_Locations_Rack] DEFAULT '',
        [Level] nvarchar(10) NOT NULL CONSTRAINT [DF_Locations_Level] DEFAULT '',
        [Position] nvarchar(10) NOT NULL CONSTRAINT [DF_Locations_Position] DEFAULT '',
        [Capacity] decimal(18,2) NOT NULL CONSTRAINT [DF_Locations_Capacity] DEFAULT 0,
        [CurrentCapacity] decimal(18,2) NOT NULL CONSTRAINT [DF_Locations_CurrentCapacity] DEFAULT 0,
        [AllowMixedProducts] bit NOT NULL CONSTRAINT [DF_Locations_AllowMixedProducts] DEFAULT 0,
        [Status] tinyint NOT NULL CONSTRAINT [DF_Locations_Status] DEFAULT 1,
        [IsActive] bit NOT NULL CONSTRAINT [DF_Locations_IsActive] DEFAULT 1,
        [CreatedAt] datetime NOT NULL CONSTRAINT [DF_Locations_CreatedAt] DEFAULT GETDATE(),
        [UpdatedAt] datetime NOT NULL CONSTRAINT [DF_Locations_UpdatedAt] DEFAULT GETDATE()
    );
    CREATE UNIQUE INDEX [IX_Locations_WarehouseCode] ON [Locations]([WarehouseId], [Code]);
END");
        }

        private async Task EnsureDefaultWarehouseAsync()
        {
            await EnsureWarehousesTableAsync();
            if (!await context.Warehouses.AnyAsync(w => w.Name == "Principal"))
            {
                context.Warehouses.Add(new Warehouse { Name = "Principal" });
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Warehouse>> GetWarehousesAsync()
        {
            await EnsureDefaultWarehouseAsync();
            return await context.Warehouses.OrderBy(w => w.Name).ToListAsync();
        }

        public async Task<List<Location>> GetLocationsAsync()
        {
            await EnsureLocationsTableAsync();
            var data = await (from l in context.Locations
                              join w in context.Warehouses on l.WarehouseId equals w.WarehouseId into wj
                              from w in wj.DefaultIfEmpty()
                              orderby l.Code
                              select new Location
                              {
                                  LocationId = l.LocationId,
                                  WarehouseId = l.WarehouseId,
                                  Code = l.Code,
                                  Barcode = l.Barcode,
                                  BarcodeType = l.BarcodeType,
                                  Zone = l.Zone,
                                  Aisle = l.Aisle,
                                  Rack = l.Rack,
                                  Level = l.Level,
                                  Position = l.Position,
                                  Capacity = l.Capacity,
                                  CurrentCapacity = l.CurrentCapacity,
                                  AllowMixedProducts = l.AllowMixedProducts,
                                  Status = l.Status,
                                  IsActive = l.IsActive,
                                  CreatedAt = l.CreatedAt,
                                  UpdatedAt = l.UpdatedAt,
                                  WarehouseName = w != null ? w.Name : string.Empty
                              }).ToListAsync();
            return data;
        }

        public async Task<Location?> CreateLocationAsync(Location location)
        {
            await EnsureLocationsTableAsync();
            await EnsureDefaultWarehouseAsync();
            if (location == null || string.IsNullOrWhiteSpace(location.Code)) return null;

            var code = location.Code.Trim();
            if (string.IsNullOrWhiteSpace(code)) return null;

            if (await context.Locations.AnyAsync(l => l.WarehouseId == location.WarehouseId && l.Code.ToLower() == code.ToLower()))
                return null;

            location.Code = code;
            location.Barcode = location.Barcode?.Trim() ?? string.Empty;
            location.BarcodeType = location.BarcodeType?.Trim() ?? string.Empty;
            location.Zone = location.Zone?.Trim() ?? string.Empty;
            location.Aisle = location.Aisle?.Trim() ?? string.Empty;
            location.Rack = location.Rack?.Trim() ?? string.Empty;
            location.Level = location.Level?.Trim() ?? string.Empty;
            location.Position = location.Position?.Trim() ?? string.Empty;
            location.CreatedAt = DateTime.Now;
            location.UpdatedAt = DateTime.Now;
            context.Locations.Add(location);
            await context.SaveChangesAsync();
            return location;
        }

        public async Task<Location?> UpdateLocationAsync(int id, Location location)
        {
            await EnsureLocationsTableAsync();
            if (location == null || string.IsNullOrWhiteSpace(location.Code)) return null;

            var existing = await context.Locations.FindAsync(id);
            if (existing == null) return null;

            var code = location.Code.Trim();
            if (string.IsNullOrWhiteSpace(code)) return null;

            if (await context.Locations.AnyAsync(l => l.LocationId != id && l.WarehouseId == location.WarehouseId && l.Code.ToLower() == code.ToLower()))
                return null;

            existing.WarehouseId = location.WarehouseId;
            existing.Code = code;
            existing.Barcode = location.Barcode?.Trim() ?? string.Empty;
            existing.BarcodeType = location.BarcodeType?.Trim() ?? string.Empty;
            existing.Zone = location.Zone?.Trim() ?? string.Empty;
            existing.Aisle = location.Aisle?.Trim() ?? string.Empty;
            existing.Rack = location.Rack?.Trim() ?? string.Empty;
            existing.Level = location.Level?.Trim() ?? string.Empty;
            existing.Position = location.Position?.Trim() ?? string.Empty;
            existing.Capacity = location.Capacity;
            existing.CurrentCapacity = location.CurrentCapacity;
            existing.AllowMixedProducts = location.AllowMixedProducts;
            existing.Status = location.Status;
            existing.IsActive = location.IsActive;
            existing.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            await EnsureLocationsTableAsync();
            var location = await context.Locations.FindAsync(id);
            if (location == null) return false;
            context.Locations.Remove(location);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductUnitAsync(int id)
        {
            await EnsureProductUnitsTableAsync();
            var unit = await context.ProductUnits.FindAsync(id);
            if (unit == null) return false;
            context.ProductUnits.Remove(unit);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
