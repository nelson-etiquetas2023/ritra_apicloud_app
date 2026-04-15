# Documentación: Sistema de Manejo de Imágenes - Blazor WebAssembly + ASP.NET Core API

## 📋 Descripción General

Este documento describe el flujo completo de integración de manejo de imágenes en la aplicación Blazor WebAssembly que se conecta a una API ASP.NET Core. Las imágenes se guardan en una carpeta `uploads` con nombres aleatorios por seguridad, se registran en la base de datos con Entity Framework, y se visualizan en el componente Razor.

---

## 🏗️ Estructura del Proyecto

```
Solución
├── API/
│   ├── Controllers/
│   │   └── UploadController.cs        # Controlador API para manejo de imágenes
│   ├── Services/
│   │   └── Upload/
│   │       ├── IUploadService.cs      # Interfaz del servicio
│   │       └── UploadService.cs       # Implementación del servicio
│   ├── Data/
│   │   └── ApplicationDbContext.cs    # DbContext (contiene DbSet<UploadResult>)
│   └── Program.cs                     # Configuración de la API
├── WEB/ (Blazor WebAssembly)
│   ├── Services/
│   │   └── Upload/
│   │       └── UploadService.cs       # Servicio HTTP para consumir la API
│   └── Pages/
│       └── Upload/
│           └── Index.razor            # Componente de interfaz de usuario
├── Shared/
│   └── Dtos/
│       └── UploadResult.cs            # DTO compartido
```

---

## 🗄️ Base de Datos

### Tabla: Uploads

La tabla `Uploads` se crea automáticamente mediante Entity Framework con la siguiente estructura:

```csharp
public class UploadResult
{
    public int Id { get; set; }                    // Identificador único
    public string? FileName { get; set; }          // Nombre original del archivo
    public string? StoredFileName { get; set; }    // Nombre aleatorio en el servidor
    public string? ContentType { get; set; }       // Tipo MIME (ej: image/jpeg)
}
```

**Campos:**
- **Id**: Clave primaria, se auto-incrementa
- **FileName**: Nombre original del archivo subido por el usuario
- **StoredFileName**: Nombre generado aleatoriamente (seguridad)
- **ContentType**: Tipo de contenido (MIME type)

---

## 🔧 Componentes Implementados

### 1. API - UploadController

**Endpoints disponibles:**

#### POST `/api/upload/uploadfile`
Carga múltiples imágenes.

**Request:**
- Multipart form data con lista de archivos
- Campo: `files` (IFormFile[])

**Response:**
```json
[
  {
    "id": 1,
    "fileName": "foto.jpg",
    "storedFileName": "a1b2c3d4e5f6.tmp",
    "contentType": "image/jpeg"
  }
]
```

#### GET `/api/upload/getimages`
Obtiene todas las imágenes registradas.

**Response:**
```json
[
  {
    "id": 1,
    "fileName": "foto.jpg",
    "storedFileName": "a1b2c3d4e5f6.tmp",
    "contentType": "image/jpeg"
  },
  {
    "id": 2,
    "fileName": "documento.png",
    "storedFileName": "x7y8z9w0q1r2.tmp",
    "contentType": "image/png"
  }
]
```

#### GET `/api/upload/getimagenbyid?id={id}`
Descarga una imagen específica por su ID.

**Response:** Archivo binario con headers apropiados

---

### 2. Servicio de Upload - Blazor (WEB)

**Métodos disponibles:**

```csharp
// Cargar múltiples imágenes
public async Task<List<UploadResult>> UploadFile(MultipartFormDataContent files)

// Obtener todas las imágenes
public async Task<List<UploadResult>> GetAllImages()

// Obtener imagen por ID (retorna bytes)
public async Task<byte[]> GetImageById(int id)
```

---

### 3. Componente Razor - Upload/Index.razor

**Características:**

- 📤 **Input de archivo múltiple**: Selector de imágenes con filtro `accept="image/*"`
- 🔄 **Carga automática**: Las imágenes se cargan al iniciar el componente
- 🖼️ **Galería de imágenes**: Visualización en grid responsivo (Bootstrap)
- 📊 **Estados de carga**: Spinner mientras se cargan imágenes
- ✅ **Mensajes de feedback**: Confirmación de carga exitosa o error
- 🎨 **Interfaz moderna**: Tarjetas Bootstrap con información detallada

**Flujo del componente:**
1. Al cargar: Obtiene todas las imágenes guardadas de la API
2. Usuario selecciona una o más imágenes
3. Se envían a la API mediante `MultipartFormDataContent`
4. Se muestran en la galería en tiempo real
5. Cada imagen muestra: nombre original, nombre almacenado e ID

---

## 🚀 Uso

### Para el usuario final:

1. **Navegar a la página**: `/Upload`
2. **Cargar imágenes**:
   - Hacer clic en "Selecciona una o más imágenes"
   - Seleccionar una o más archivos de imagen
   - Las imágenes se cargarán automáticamente
3. **Ver galería**: Las imágenes cargadas aparecen en tarjetas abajo del input
4. **Ver detalles**: Cada tarjeta muestra nombre original y nombre almacenado

---

## 🛡️ Seguridad

### Medidas implementadas:

1. **Nombres aleatorios**: Las imágenes se guardan con nombres generados aleatoriamente
   ```csharp
   trustedFileNameForFileStorage = Path.GetRandomFileName();
   ```

2. **Validación de tipo**: Se valida el `ContentType` en la API

3. **Isolamiento de carpeta**: Las imágenes se guardan en una carpeta dedicada `uploads`

4. **Limitación de acceso**: 
   - Solo se puede acceder a imágenes registradas en BD
   - El ID es requerido para descargar

---

## 📦 Instalación y Configuración

### 1. Base de datos

La tabla se crea automáticamente mediante migrations. Si necesitas crear manualmente:

```sql
CREATE TABLE [Uploads] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [FileName] NVARCHAR(MAX),
    [StoredFileName] NVARCHAR(MAX),
    [ContentType] NVARCHAR(MAX)
);
```

### 2. Carpeta de uploads

Se crea automáticamente en `API/uploads` cuando se inicia la aplicación.

### 3. Configuración en Program.cs (API)

Ya está implementada:
- ✅ Servicio registrado en DI
- ✅ Carpeta creada automáticamente
- ✅ StaticFiles middleware configurado

### 4. HttpClient (Blazor)

Verifica que en `Program.cs` del WEB esté configurado:

```csharp
builder.Services.AddHttpClient("ritrama", client =>
{
    client.BaseAddress = new Uri("https://localhost:7000/"); // Ajusta según tu API
});
```

---

## 🐛 Solución de Problemas

### Las imágenes no se cargan
- ✓ Verifica que la carpeta `uploads` existe en `API/`
- ✓ Comprueba los permisos de escritura
- ✓ Revisa los logs en Visual Studio

### Error 404 al descargar imágenes
- ✓ El registro está en la BD pero no el archivo en disco
- ✓ Verifica que el archivo existe en `API/uploads/{storedFileName}`

### Las imágenes no se guardan en BD
- ✓ Comprueba la conexión a la BD
- ✓ Revisa si hay permisos en la tabla Uploads

---

## 📝 Ejemplo de Integración en Otro Componente

Si deseas usar el manejo de imágenes en otro componente:

```razor
@page "/MiComponente"
@inject UploadService UploadService

<InputFile OnChange="@(e => CargarImagen(e))" accept="image/*" />

@if (imagenActual != null)
{
    <img src="@imagenActual" style="max-width: 300px;" />
}

@code {
    private string? imagenActual;

    private async Task CargarImagen(InputFileChangeEventArgs e)
    {
        var file = e.File;
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(5242880)); // 5MB
        fileContent.Headers.ContentType = 
            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "\"files\"", file.Name);

        var resultado = await UploadService.UploadFile(content);
        if (resultado?.Any() == true)
        {
            var imagenId = resultado.First().Id;
            imagenActual = $"api/upload/getimagenbyid?id={imagenId}";
        }
    }
}
```

---

## 🎯 Flujo Completo - Diagrama

```
Usuario
   │
   ├─→ Selecciona imágenes (InputFile)
   │
   ├─→ UploadService.UploadFile()
   │
   ├─→ API POST /api/upload/uploadfile
   │
   ├─→ UploadController.UploadFile()
   │
   ├─→ UploadService.UploadFilesAsync()
   │   ├─→ Genera nombre aleatorio
   │   ├─→ Guarda en /uploads/
   │   └─→ Guarda en BD (Uploads table)
   │
   ├─→ Response: List<UploadResult>
   │
   └─→ Componente renderiza imágenes
       ├─→ Muestra en galería
       └─→ GET /api/upload/getimagenbyid?id={id} para mostrar
```

---

## 📊 Rendimiento

- **Máximo de archivos**: Sin límite (configurable)
- **Tamaño máximo por archivo**: 5 MB (configurable en Blazor)
- **Base de datos**: Consultas optimizadas con LINQ

---

## 🔄 Próximas Mejoras (Opcionales)

- [ ] Eliminación de imágenes (soft delete)
- [ ] Compresión de imágenes
- [ ] Generación de thumbnails
- [ ] Drag and drop
- [ ] Progress bar de carga
- [ ] Validación de tamaño y formato más estricta
- [ ] Autorización por usuario
- [ ] Categorización de imágenes

---

## 📞 Soporte

Si tienes problemas, revisa:
1. Los logs en Visual Studio
2. La consola del navegador (F12)
3. La carpeta `uploads` en el servidor
4. La tabla `Uploads` en la BD

