# 🔧 Troubleshooting - Solución de Problemas

## ❌ Error: "Archivo no encontrado (404)"

### Síntomas:
- La imagen no se muestra en el navegador
- Error 404 en Network tab

### Causas posibles:
1. El archivo no existe en la carpeta `uploads/`
2. La ruta es incorrecta
3. El registro está en BD pero el archivo se perdió

### Solución:
```bash
# 1. Verifica que la carpeta existe
ls API/uploads/

# 2. Verifica que hay archivos
ls API/uploads/ | wc -l

# 3. Verifica la ruta en el controlador
# En UploadController.cs, línea de GetImagen()
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", upload.StoredFileName);
```

---

## ❌ Error: "CORS Error"

### Síntomas:
```
Access to XMLHttpRequest at 'https://localhost:7000/...' from origin 
'https://localhost:7052' has been blocked by CORS policy
```

### Solución:
En `API/Program.cs`, verifica que CORS está configurado:

```csharp
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

// Después de crear app:
app.UseCors("RitramaCors");
```

---

## ❌ Error: "No guarda en la base de datos"

### Síntomas:
- Archivo se guarda pero no aparece en la tabla `Uploads`
- Sin registros en BD

### Checklist:
```sql
-- 1. Verifica que la tabla existe
SELECT * FROM sys.tables WHERE name = 'Uploads';

-- 2. Verifica registros
SELECT COUNT(*) FROM Uploads;

-- 3. Verifica datos
SELECT * FROM Uploads;
```

### Solución:
```csharp
// En API/Program.cs, asegúrate que la BD se crea:
using (var scope = app.Services.CreateScope())
{
    var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbcontext.Database.EnsureCreated();  // Crear tablas automáticamente
}
```

---

## ❌ Error: "No puede crear carpeta uploads"

### Síntomas:
- Error al crear archivo en la carpeta
- "Access denied" o "Directory not found"

### Solución:
```csharp
// En API/Program.cs
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");

// Asegúrate que tienes permisos
if (!Directory.Exists(uploadsPath))
{
    try
    {
        Directory.CreateDirectory(uploadsPath);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creando carpeta: {ex.Message}");
        // Alternativa: usar carpeta temporal
        uploadsPath = Path.Combine(Path.GetTempPath(), "uploads");
        Directory.CreateDirectory(uploadsPath);
    }
}
```

---

## ❌ Error: "HttpClient no está configurado"

### Síntomas:
```
NullReferenceException: Object reference not set to an instance of an object.
```

### Solución:
En `WEB/Program.cs`:
```csharp
builder.Services.AddHttpClient("ritrama", client =>
{
    client.BaseAddress = new Uri("https://localhost:7000/");
});
```

---

## ❌ Error: "Las imágenes no se cargan al inicializar"

### Síntomas:
- Galería vacía al abrir la página
- No muestra imágenes existentes

### Solución:
```razor
// En WEB/Pages/Upload/Index.razor
protected override async Task OnInitializedAsync()
{
    try
    {
        await LoadImagesAsync();
    }
    catch (Exception ex)
    {
        uploadMessage = $"Error: {ex.Message}";
    }
}

private async Task LoadImagesAsync()
{
    isLoadingImages = true;
    uploadResults = await Uploadservice.GetAllImages();
    isLoadingImages = false;
}
```

---

## ❌ Error: "Timeout al subir archivos grandes"

### Síntomas:
- Carga se queda "pegada"
- Timeout después de 30 segundos

### Solución:
```csharp
// En WEB/Program.cs
builder.Services.AddHttpClient("ritrama", client =>
{
    client.BaseAddress = new Uri("https://localhost:7000/");
    client.Timeout = TimeSpan.FromMinutes(5); // Aumentar timeout
});
```

También en `WEB/Pages/Upload/Index.razor`:
```csharp
public long maxFileSize = 10485760; // 10 MB (aumentar si es necesario)
```

---

## ❌ Error: "InvalidOperationException: no se puede inferir tipo"

### Síntomas:
```
CS0411: Los argumentos de tipo para el método ToListAsync no se pueden inferir
```

### Solución:
Asegúrate que tengas el using en `API/Services/Upload/UploadService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
```

---

## ❌ Error: "El archivo es muy grande"

### Síntomas:
```
Request entity too large (413)
```

### Solución:
En `API/Program.cs`:
```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
```

---

## ❌ Error: "UploadService no se encuentra"

### Síntomas:
```
InvalidOperationException: Unable to resolve service for type 'API.Services.Upload.IUploadService'
```

### Solución:
En `API/Program.cs`:
```csharp
// DEBE estar ANTES de app.Build()
builder.Services.AddScoped<IUploadService, UploadService>();

// Y el using:
using API.Services.Upload;
```

---

## ❌ Error: "No se pueden mostrar imágenes en desarrollo"

### Síntomas:
- Funciona en producción pero no en desarrollo
- Imágenes en caché

### Solución:
```bash
# Limpia caché del navegador
Ctrl + Shift + Delete

# En Visual Studio: menú Tools
Tools → Delete Browser Cache

# Ejecuta sin caché
dotnet run --no-cache

# O en Blazor en el _Imports.razor, agrega:
@using System.Diagnostics

# En el componente:
@{
    var nocache = DateTime.Now.Ticks;
}

<img src="api/upload/getimagenbyid?id=@image.Id&v=@nocache" />
```

---

## ✅ Checklist de Depuración

Cuando algo no funcione, revisa en orden:

- [ ] ¿La API está ejecutándose? (`dotnet run` en carpeta API)
- [ ] ¿El puerto es correcto? (por defecto 7000)
- [ ] ¿La carpeta `uploads` existe en `API/`?
- [ ] ¿La tabla `Uploads` existe en BD? (ejecutar migrations)
- [ ] ¿CORS está configurado? (verificar Program.cs API)
- [ ] ¿HttpClient está configurado? (verificar Program.cs WEB)
- [ ] ¿El navegador tiene caché? (limpiar con Ctrl+Shift+Del)
- [ ] ¿Hay errores en consola? (F12 → Console)
- [ ] ¿Hay errores en Network? (F12 → Network)
- [ ] ¿Los permisos de carpeta son correctos? (Windows Explorer)

---

## 📊 Logs Útiles

### Ver logs de la API
```bash
# En Visual Studio, abre Output window (Ctrl+Alt+O)
# Selecciona "API" en el dropdown

# O en terminal:
dotnet run --verbose
```

### Ver logs del navegador
```javascript
// F12 → Console → ejecuta:
fetch('https://localhost:7000/api/upload/getimages')
    .then(r => r.json())
    .then(d => console.log(d))
    .catch(e => console.error(e));
```

### Ver directorio de uploads
```bash
# PowerShell
cd C:\Programacion\RitramaCloud2026\API
ls uploads

# O en C#:
var path = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
Console.WriteLine($"Ruta: {path}");
Console.WriteLine($"Existe: {Directory.Exists(path)}");
Console.WriteLine($"Archivos: {Directory.GetFiles(path).Length}");
```

---

## 🔍 Debugging Avanzado

### Ver solicitud HTTP completa
```javascript
// En consola del navegador (F12):
fetch('https://localhost:7000/api/upload/getimages', {
    method: 'GET',
    headers: {
        'Content-Type': 'application/json'
    }
})
.then(response => {
    console.log('Status:', response.status);
    console.log('Headers:', response.headers);
    return response.json();
})
.then(data => console.log('Data:', data))
.catch(error => console.error('Error:', error));
```

### Ver variables en C#
```csharp
// En UploadService.cs, agrega debuggers:
public async Task<UploadResult> UploadFilesAsync(List<IFormFile> files)
{
    foreach (var file in files)
    {
        System.Diagnostics.Debug.WriteLine($"Archivo: {file.FileName}");
        System.Diagnostics.Debug.WriteLine($"Tamaño: {file.Length}");
        System.Diagnostics.Debug.WriteLine($"ContentType: {file.ContentType}");
    }
}

// Ve Output window para ver los mensajes
```

---

## 💡 Tips de Desarrollo

1. **Usa la paleta de colores del navegador:**
   ```html
   <!-- En browser DevTools -->
   document.body.style.backgroundColor = '#f0f0f0';
   ```

2. **Simula error de red:**
   ```javascript
   // Desactiva todos los fetch
   window.fetch = () => Promise.reject('Simulado');
   ```

3. **Verifica la estructura de datos:**
   ```javascript
   JSON.stringify(tuVariable, null, 2)
   ```

4. **Pausar en breakpoint:**
   - En Visual Studio: Click a la izquierda del código
   - Ejecuta `dotnet run`
   - El debugger pausará cuando llegue al punto

---

## 📞 Contacto para Soporte

Si el problema persiste:
1. Revisa los 3 archivos de documentación incluidos
2. Ejecuta el checklist anterior
3. Verifica los logs en Output window
4. Compila desde cero: `dotnet clean && dotnet build`

---

**¡Espero que esto te ayude a resolver problemas! 🚀**
