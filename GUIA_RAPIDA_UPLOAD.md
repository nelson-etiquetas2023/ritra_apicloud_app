# 🚀 Guía Rápida - Sistema de Upload de Imágenes

## ⚡ Inicio Rápido (5 minutos)

### 1️⃣ Verificar Estructura (Ya Implementada)
✅ Carpeta `API/Services/Upload/` con servicios  
✅ Carpeta `API/Controllers/UploadController.cs`  
✅ Tabla `Uploads` en la base de datos  
✅ Componente `WEB/Pages/Upload/Index.razor`  
✅ Servicio `WEB/Services/Upload/UploadService.cs`  

### 2️⃣ Asegurar Configuración en Program.cs (API)

**Verificar que existe:**
```csharp
builder.Services.AddScoped<IUploadService, UploadService>();
```

**Verificar que la carpeta se crea:**
```csharp
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
```

### 3️⃣ Asegurar Configuración de HttpClient (WEB/Program.cs)

Debe tener:
```csharp
builder.Services.AddHttpClient("ritrama", client =>
{
    client.BaseAddress = new Uri("https://localhost:7000/"); // Ajusta según tu puerto
});
```

### 4️⃣ Navegar a la Página

En tu `NavMenu.razor` o `MainLayout.razor`, asegúrate de tener un enlace a:
```html
<a href="/Upload">Subir Imágenes</a>
```

### 5️⃣ Ejecutar la Aplicación

```bash
# Terminal 1 - API
cd API
dotnet run

# Terminal 2 - Blazor WebAssembly
cd WEB
dotnet run
```

---

## 📊 Flujo de Uso

```
1. Usuario abre: https://localhost:7052/Upload
   ↓
2. Componente carga imágenes existentes
   ↓
3. Usuario selecciona imágenes (InputFile)
   ↓
4. Se envían a API: POST /api/upload/uploadfile
   ↓
5. API genera nombre aleatorio y guarda en /uploads/
   ↓
6. Se registra en tabla Uploads (BD)
   ↓
7. API retorna lista de UploadResult
   ↓
8. Componente renderiza galería con imágenes
```

---

## 🎯 Funcionalidades Principales

### ✅ Cargar Imágenes
- Input múltiple (selecciona varias a la vez)
- Filtro de solo imágenes (`accept="image/*"`)
- Envío automático a la API

### ✅ Mostrar Galería
- Grid responsivo (adapta a pantalla)
- Carga automática al iniciar componente
- Visualización en tarjetas con información

### ✅ Seguridad
- Nombres aleatorios (no predecibles)
- Validación en servidor
- Registros en BD

---

## 🔍 Verificar que Todo Funciona

### 1. Base de Datos
```sql
-- Ejecutar en SQL Server
SELECT * FROM Uploads;
```

Debe mostrar registros insertados.

### 2. Carpeta de Archivos
```
API/uploads/
├── a1b2c3d4e5f6.tmp (archivo 1)
├── x7y8z9w0q1r2.tmp (archivo 2)
└── ...
```

### 3. API REST
```
GET https://localhost:7000/api/upload/getimages
```

Debe retornar JSON con lista de imágenes.

### 4. Componente Blazor
Navega a `https://localhost:7052/Upload` y verifica:
- ✓ Se cargan las imágenes existentes
- ✓ Puedes seleccionar nuevas imágenes
- ✓ Aparecen en la galería después de cargar

---

## ⚙️ Configuración Personalizada

### Cambiar Carpeta de Uploads
En `API/Program.cs`:
```csharp
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "mi-carpeta-personalizada");
```

### Cambiar Puerto API
En `API/launchSettings.json` y en `WEB/Program.cs` HttpClient

### Aumentar Límite de Tamaño
En `WEB/Pages/Upload/Index.razor`:
```csharp
public long maxFileSize = 52428800; // 50 MB
```

---

## 🐛 Solución Rápida de Problemas

| Problema | Solución |
|----------|----------|
| Error 404 en imágenes | Verifica que archivo existe en `API/uploads/` |
| No guarda en BD | Comprueba conexión a base de datos |
| CORS Error | Verifica configuración en `Program.cs` API |
| Timeout al subir | Aumenta `maxFileSize` o comprime imágenes |
| No aparecen imágenes cargadas | Limpia caché del navegador (Ctrl+Shift+Del) |

---

## 📝 Ejemplo de Uso en Otro Componente

```razor
@page "/MiPagina"
@inject UploadService UploadService

<h3>Mi Componente</h3>

<InputFile OnChange="@Subir" accept="image/*" />

<img src="@imagenUrl" style="max-width: 300px;" />

@code {
    private string? imagenUrl;

    private async Task Subir(InputFileChangeEventArgs e)
    {
        using var content = new MultipartFormDataContent();
        var file = e.File;
        var fileContent = new StreamContent(file.OpenReadStream(5242880));
        fileContent.Headers.ContentType = 
            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "\"files\"", file.Name);

        var resultado = await UploadService.UploadFile(content);
        imagenUrl = $"api/upload/getimagenbyid?id={resultado.First().Id}";
    }
}
```

---

## 📚 Archivos Clave

| Archivo | Descripción |
|---------|-------------|
| `API/Controllers/UploadController.cs` | Endpoints de carga |
| `API/Services/Upload/UploadService.cs` | Lógica de carga en servidor |
| `API/Services/Upload/IUploadService.cs` | Interfaz del servicio |
| `WEB/Services/Upload/UploadService.cs` | Cliente HTTP (Blazor) |
| `WEB/Pages/Upload/Index.razor` | Interfaz de usuario |
| `WEB/Pages/Upload/Index.razor.css` | Estilos CSS |
| `Shared/Dtos/UploadResult.cs` | Modelo de datos |

---

## ✨ Próximos Pasos (Opcional)

- [ ] Agregar validación de tamaño mínimo/máximo
- [ ] Implementar eliminación de imágenes
- [ ] Agregar drag and drop
- [ ] Crear thumbnails
- [ ] Comprimir imágenes automáticamente
- [ ] Agregar categorías a imágenes
- [ ] Implementar búsqueda y filtrado

---

## 💡 Tips

✓ Usa Dev Tools (F12) para ver errores en consola  
✓ Revisa Application tab → Storage → Local Storage para datos  
✓ En Network tab puedes ver las requests HTTP  
✓ Limpia caché si no ves cambios: Ctrl+Shift+Del  
✓ Usa Dark Mode en VS para menos fatiga visual 😎

---

**¡Listo! El sistema está completo y funcional. Disfruta! 🎉**
