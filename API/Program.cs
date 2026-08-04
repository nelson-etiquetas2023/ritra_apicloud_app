using API.Data;
using API.Services.AppMovil;
using API.Services.Auth;
using API.Services.Config;
using API.Services.Inventory;
using API.Services.OcMovil;
using API.Services.Products;
using API.Services.Reports;
using API.Services.Upload;
using API.Services.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Confguraci�n de CORS.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://192.168.10.10:9000", 
            "https://192.168.10.10:9000",
            "http://192.168.10.26:5094",
            "https://192.168.10.26:5094",
            "http://localhost:9000",
            "https://localhost:9000")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
try
{
    var connectionString = builder.Configuration.GetConnectionString("SERVIDOR-ETIQUETA");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Cadena de conexi�n 'SERVIDOR-ETIQUETA' no encontrada en appsettings.json");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
catch (Exception ex)
{
   Console.WriteLine($"Error al configurar la base de datos: {ex.Message}");
}

//Inyeccion de mis servicios.
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IAppMovilService, AppMovilService>();
builder.Services.AddScoped<IOcMovilService, OcMovilService>();

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var secret = builder.Configuration.GetSection("AppSettings:Token").Value;
if (!string.IsNullOrEmpty(secret))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
}

builder.Services.AddControllers();

var app = builder.Build();

//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
//        Path.Combine(app.Environment.ContentRootPath, "uploads")),
//    RequestPath = "/uploads"
//});
// Crear carpeta de uploads si no existe
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
//seeder
try
{
    using var scope = app.Services.CreateScope();
    var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbcontext.Database.Migrate();
    DataSeeder.Seed(dbcontext);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error en DataSeeder");
}

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

