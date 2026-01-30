using API.Data;
using API.Services.Inventory;
using API.Services.Products;
using API.Services.Users;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => 
options.UseSqlServer(builder.Configuration.GetConnectionString("Connection-Monsters")));

builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();



builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

//seeder
//using (var scope = app.Services.CreateScope())
//{
//    var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    dbcontext.Database.EnsureCreated();
//    DataSeeder.Seed(dbcontext);
//}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

app.UseAuthorization();

app.MapControllers();

app.Run();
