# 🚀 Guía de Despliegue a Producción

## 📋 Checklist Pre-Despliegue

### Backend (API)

- [ ] Cambiar connection string a base de datos de producción
- [ ] Actualizar puertos de CORS
- [ ] Configurar variables de entorno
- [ ] Crear carpeta `uploads` con permisos apropiados
- [ ] Verificar límites de tamaño de archivo
- [ ] Configurar backups de la carpeta `uploads`
- [ ] Establecer política de limpieza de archivos antiguos
- [ ] Habilitar compresión en IIS
- [ ] Configurar HTTPS/SSL
- [ ] Revisar logs y monitoreo

### Frontend (Blazor)

- [ ] Cambiar URL base de API a URL de producción
- [ ] Deshabilitar modo DEBUG
- [ ] Minificar CSS y JavaScript
- [ ] Optimizar imágenes estáticas
- [ ] Configurar cache policies
- [ ] Revisar service worker (PWA)
- [ ] Pruebas en navegadores finales

---

## 🔧 Configuración para Producción

### 1. API - Program.cs

```csharp
// Cambiar origen CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("RitramaCors", policy =>
    {
        // En producción, especificar origen exacto
        policy.WithOrigins("https://tudominio.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Aumentar límites para producción
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 104857600; // 100 MB
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

// Connection string desde variables de entorno
var connectionString = builder.Configuration["ConnectionStrings:Production"]
    ?? throw new InvalidOperationException("Connection string not found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Logging en producción
if (!app.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}
```

### 2. WEB - Program.cs

```csharp
// URL de API desde configuración
var apiUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? "https://api.tudominio.com";

builder.Services.AddHttpClient("ritrama", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Caché y rendimiento
builder.Services.AddScoped<ILocalStorage, LocalStorage>();
```

### 3. appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "SERVIDOR-ETIQUETA": "Server=prod-server;Database=RitramaDB;Trusted_Connection=true;"
  },
  "ApiSettings": {
    "BaseUrl": "https://api.tudominio.com"
  }
}
```

---

## 🗄️ Base de Datos - Producción

### 1. Migración de Datos

```bash
# Crear backup
BACKUP DATABASE RitramaDB 
TO DISK = 'C:\Backups\RitramaDB_backup.bak'

# Aplicar migrations
dotnet ef database update --configuration Release

# Verificar tabla Uploads
SELECT * FROM Uploads;
```

### 2. Configurar Mantenimiento

```sql
-- Crear índices para mejor rendimiento
CREATE NONCLUSTERED INDEX IX_Uploads_StoredFileName 
ON Uploads (StoredFileName);

CREATE NONCLUSTERED INDEX IX_Uploads_CreatedDate 
ON Uploads (CreatedDate)
WHERE DeletedDate IS NULL;

-- Procedimiento para limpiar archivos antiguos
CREATE PROCEDURE sp_CleanupOldUploads
    @DaysOld INT = 30
AS
BEGIN
    DELETE FROM Uploads 
    WHERE DATEDIFF(DAY, CreatedDate, GETDATE()) > @DaysOld
END

-- Configurar job para ejecutar cada semana
EXEC sp_add_schedule @schedule_name=N'CleanupSchedule',
    @freq_type=8, @freq_interval=1, @active_start_time=020000
```

### 3. Script de Limpieza Automática en C#

```csharp
// En API/Services/Upload/UploadService.cs

public async Task<bool> CleanupOldFilesAsync(int daysOld = 30)
{
    try
    {
        var cutoffDate = DateTime.Now.AddDays(-daysOld);
        var oldUploads = await _context.Uploads
            .Where(u => u.CreatedDate < cutoffDate)
            .ToListAsync();

        foreach (var upload in oldUploads)
        {
            var filePath = Path.Combine(
                _environment.ContentRootPath, 
                "uploads", 
                upload.StoredFileName);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        _context.Uploads.RemoveRange(oldUploads);
        await _context.SaveChangesAsync();
        return true;
    }
    catch
    {
        return false;
    }
}
```

---

## 🌐 Despliegue en IIS

### 1. Preparar Aplicación

```bash
# Compilar en modo Release
dotnet publish -c Release -o ./publish

# Crear archivos .zip
cd publish
Compress-Archive -Path * -DestinationPath ../api.zip
```

### 2. Configurar en IIS

1. Abrir IIS Manager
2. Clic derecho en "Sites" → "Add Website"
3. Nombre: RitramaAPI
4. Physical path: `C:\inetpub\wwwroot\ritra-api`
5. Binding: `tudominio.com` puerto 443 (HTTPS)
6. SSL certificate: Asignar certificado

### 3. Permisos de Carpeta

```bash
# En PowerShell como Admin
$iisUser = "IIS AppPool\RitramaAPI"
$uploadsPath = "C:\inetpub\wwwroot\ritra-api\uploads"

# Otorgar permisos
icacls $uploadsPath /grant:f "${iisUser}:(OI)(CI)F"
```

### 4. Configurar Web.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <httpProtocol>
      <customHeaders>
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-Frame-Options" value="DENY" />
        <add name="X-XSS-Protection" value="1; mode=block" />
      </customHeaders>
    </httpProtocol>

    <staticContent>
      <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
    </staticContent>

    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>

    <aspNetCore processPath="dotnet" arguments=".\API.dll" stdoutLogEnabled="true" 
                stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
  </system.webServer>
</configuration>
```

---

## 🔒 Seguridad en Producción

### 1. HTTPS/SSL

```csharp
// En Program.cs (API)
app.UseHttpsRedirection();

// Agregar HSTS
app.UseHsts();
```

### 2. Rate Limiting

```csharp
// En Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", configure: options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });
});

// Usar en controller
[RequireRateLimiting("fixed")]
[HttpPost("uploadfile")]
public async Task<ActionResult<List<UploadResult>>> UploadFile([FromForm] List<IFormFile> files)
```

### 3. Validación Stricta

```csharp
// En UploadService.cs
public async Task<List<UploadResult>> UploadFilesAsync(List<IFormFile> files)
{
    var allowedMimeTypes = new[] 
    { 
        "image/jpeg", 
        "image/png", 
        "image/gif", 
        "image/webp" 
    };

    foreach (var file in files)
    {
        // Validar tipo
        if (!allowedMimeTypes.Contains(file.ContentType))
            throw new InvalidOperationException($"Tipo no permitido: {file.ContentType}");

        // Validar extensión
        var ext = Path.GetExtension(file.FileName).ToLower();
        var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExts.Contains(ext))
            throw new InvalidOperationException($"Extensión no permitida: {ext}");

        // Validar tamaño
        if (file.Size > 52428800) // 50 MB
            throw new InvalidOperationException("Archivo demasiado grande");
    }

    // Continuar con carga...
}
```

### 4. Antivirus en Servidor

```csharp
// Integración con antivirus (ejemplo: Windows Defender)
[DllImport("msvcrt.dll")]
private static extern IntPtr memset(IntPtr dest, int c, UIntPtr count);

public async Task ScanFileAsync(string filePath)
{
    // Implementar escaneo de archivo antes de guardarlo
    // Usar ClamAV o Windows Defender API
}
```

---

## 📊 Monitoreo en Producción

### 1. Application Insights

```csharp
// En Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// O en appsettings.json
"ApplicationInsights": {
    "InstrumentationKey": "your-instrumentation-key"
}
```

### 2. Logging Centralizado

```csharp
// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.ApplicationInsights(new TelemetryClient())
);
```

### 3. Alertas

```csharp
// Monitorar carpeta uploads
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
var dirInfo = new DirectoryInfo(uploadsPath);
var diskSpace = dirInfo.GetDirectories().Length;

if (diskSpace > 1000) // Alerta si hay más de 1000 archivos
{
    logger.LogWarning("Carpeta uploads contiene muchos archivos: {count}", diskSpace);
}
```

---

## 🚀 Despliegue en Azure

### 1. Crear App Service

```bash
# Azure CLI
az group create --name ritra-group --location eastus
az appservice plan create --name ritra-plan --resource-group ritra-group --sku B2
az webapp create --resource-group ritra-group --plan ritra-plan --name ritrama-api
```

### 2. Publicar en Azure

```bash
# Desde Visual Studio
# Clic derecho en proyecto → Publish
# Seleccionar Azure App Service
# Llenar formulario y publicar

# O desde CLI
dotnet publish -c Release
az webapp up --resource-group ritra-group --name ritrama-api --plan ritra-plan
```

### 3. Configurar Storage

```csharp
// Usar Azure Blob Storage en lugar de disco local
builder.Services.AddAzureClients(builder =>
{
    builder.AddBlobServiceClient(
        new Uri("https://{storage-account}.blob.core.windows.net"),
        new DefaultAzureCredential()
    );
});
```

---

## 📈 Rendimiento

### 1. CDN para Imágenes

```csharp
// Servir imágenes desde CDN
public class UploadResult
{
    public string GetCdnUrl()
    {
        return $"https://cdn.tudominio.com/uploads/{StoredFileName}";
    }
}
```

### 2. Compresión

```csharp
// En Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "image/svg+xml" }
    );
});

app.UseResponseCompression();
```

### 3. Caché

```csharp
// En Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Cache-Control", "max-age=31536000, immutable");
    await next();
});
```

---

## ✅ Checklist Final

Antes de ir a producción:

- [ ] Base de datos migrada y respaldada
- [ ] HTTPS/SSL configurado
- [ ] CORS actualizado
- [ ] Logs configurados
- [ ] Backups automatizados
- [ ] Carpeta uploads con permisos correctos
- [ ] Validación stricta implementada
- [ ] Rate limiting configurado
- [ ] Antivirus integrado
- [ ] Monitoreo activo
- [ ] Plan de contingencia
- [ ] Documentación actualizada

---

## 🔄 CI/CD Pipeline

### GitHub Actions Ejemplo

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v2

    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '10.0.x'

    - name: Publish
      run: dotnet publish -c Release -o ./publish

    - name: Deploy to IIS
      run: |
        # Script deployment
        & "C:\Program Files\IIS\*\appcmd" stop apppool /apppool.name:"RitramaAPI"
        Copy-Item -Path ./publish/* -Destination "C:\inetpub\wwwroot\ritra-api" -Recurse -Force
        & "C:\Program Files\IIS\*\appcmd" start apppool /apppool.name:"RitramaAPI"
```

---

**¡Listo para producción! 🎉**

Para más información, consulta la documentación principal o contacta al equipo de DevOps.
