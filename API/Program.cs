using API.Data;
using API.Services.Inventory;
using API.Services.Products;
using API.Services.Users;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//Configurar los CORS.
builder.Services.AddCors(options =>
{
    options.AddPolicy("RitramaCors", policy =>
    {
        policy.WithOrigins("https://localhost:7052", "http://localhost:7052")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();

    });
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => 
options.UseSqlServer(builder.Configuration.GetConnectionString("SERVIDOR-ETIQUETA")));

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


//migraciopnes automaticas.
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//   db.Database.EnsureDeleted();  // Borra la base de datos
//    db.Database.EnsureCreated();  // La vuelve a crear según los modelos
//}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

app.UseAuthorization();

app.MapControllers();

app.UseCors("RitramaCors");

app.Run();
