# 📚 Ejemplos Avanzados de Uso

## 1️⃣ Componente con Validación

```razor
@page "/UploadConValidacion"
@using System.Net.Http.Headers
@inject UploadService UploadService

<div class="card">
    <div class="card-header">
        <h5>Subir con Validación</h5>
    </div>
    <div class="card-body">
        <div class="mb-3">
            <label class="form-label">Tamaño máximo: @(maxFileSize / 1024 / 1024) MB</label>
            <InputFile OnChange="@ValidarYSubir" accept="image/*" />
        </div>
        @if (validationErrors.Count > 0)
        {
            <div class="alert alert-warning">
                <h6>Errores de validación:</h6>
                <ul>
                    @foreach (var error in validationErrors)
                    {
                        <li>@error</li>
                    }
                </ul>
            </div>
        }
    </div>
</div>

@code {
    private long maxFileSize = 5242880; // 5 MB
    private List<string> validationErrors = [];

    private async Task ValidarYSubir(InputFileChangeEventArgs e)
    {
        validationErrors.Clear();
        var files = e.GetMultipleFiles(10); // Máximo 10 archivos

        // Validar cantidad
        if (files.Count > 10)
            validationErrors.Add("Máximo 10 archivos por vez");

        // Validar tamaño y tipo
        foreach (var file in files)
        {
            if (file.Size > maxFileSize)
                validationErrors.Add($"{file.Name}: excede {maxFileSize / 1024 / 1024}MB");

            var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!validTypes.Contains(file.ContentType))
                validationErrors.Add($"{file.Name}: tipo no permitido ({file.ContentType})");
        }

        if (validationErrors.Count == 0)
        {
            using var content = new MultipartFormDataContent();
            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "\"files\"", file.Name);
            }

            var resultado = await UploadService.UploadFile(content);
            // Procesar resultado...
        }
    }
}
```

---

## 2️⃣ Componente con Preview

```razor
@page "/UploadConPreview"
@using System.Net.Http.Headers
@inject UploadService UploadService

<div class="row">
    <div class="col-md-6">
        <h5>Seleccionar</h5>
        <InputFile OnChange="@MostrarPreview" accept="image/*" multiple />

        <div class="mt-3">
            <h6>Vista previa:</h6>
            @foreach (var preview in previews)
            {
                <div class="card mb-2">
                    <img src="@preview.DataUrl" class="card-img-top" style="max-height: 150px; object-fit: cover;" />
                    <div class="card-body">
                        <p class="card-text small">@preview.FileName</p>
                        <p class="card-text small text-muted">@(Math.Round(preview.SizeMB, 2)) MB</p>
                    </div>
                </div>
            }
        </div>

        <button class="btn btn-primary" @onclick="SubirTodos">Subir Todo</button>
    </div>
    <div class="col-md-6">
        <h5>Resultado</h5>
        @if (subidos.Count > 0)
        {
            <div class="alert alert-success">
                ✓ @subidos.Count imagen(es) cargada(s)
            </div>
        }
    </div>
</div>

@code {
    private class Preview
    {
        public string? FileName { get; set; }
        public string? DataUrl { get; set; }
        public double SizeMB { get; set; }
    }

    private List<Preview> previews = [];
    private List<UploadResult> subidos = [];

    private async Task MostrarPreview(InputFileChangeEventArgs e)
    {
        previews.Clear();

        foreach (var file in e.GetMultipleFiles(10))
        {
            var buffer = new byte[file.Size];
            await file.OpenReadStream().ReadAsync(buffer);
            var base64 = Convert.ToBase64String(buffer);

            previews.Add(new Preview
            {
                FileName = file.Name,
                DataUrl = $"data:{file.ContentType};base64,{base64}",
                SizeMB = file.Size / (1024.0 * 1024.0)
            });
        }
    }

    private async Task SubirTodos()
    {
        // Subir los archivos...
    }
}
```

---

## 3️⃣ Componente con Progreso

```razor
@page "/UploadConProgreso"
@using System.Net.Http.Headers
@inject UploadService UploadService

<div class="card">
    <div class="card-body">
        <InputFile OnChange="@SubirConProgreso" multiple />

        @if (isUploading)
        {
            <div class="mt-3">
                <p>Cargando: @currentFile de @totalFiles</p>
                <div class="progress">
                    <div class="progress-bar" role="progressbar" 
                         style="width: @(progressPercentage)%">
                        @progressPercentage%
                    </div>
                </div>
            </div>
        }
    </div>
</div>

@code {
    private bool isUploading = false;
    private int progressPercentage = 0;
    private int currentFile = 0;
    private int totalFiles = 0;

    private async Task SubirConProgreso(InputFileChangeEventArgs e)
    {
        var files = e.GetMultipleFiles();
        totalFiles = files.Count;
        isUploading = true;
        currentFile = 0;
        progressPercentage = 0;

        foreach (var file in files)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "\"files\"", file.Name);

            try
            {
                var resultado = await UploadService.UploadFile(content);
                currentFile++;
                progressPercentage = (int)((currentFile * 100) / totalFiles);
            }
            catch (Exception ex)
            {
                // Manejar error
            }
        }

        isUploading = false;
    }
}
```

---

## 4️⃣ Componente Modal para Seleccionar Imágenes

```razor
@page "/ModalSeleccionarImagen"
@inject UploadService UploadService

<button class="btn btn-primary" @onclick="AbrirModal">Seleccionar Imagen</button>

@if (mostrarModal)
{
    <div class="modal fade show d-block" style="background-color: rgba(0,0,0,0.5);">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Seleccionar Imagen</h5>
                    <button class="btn-close" @onclick="CerrarModal"></button>
                </div>
                <div class="modal-body">
                    <div class="row">
                        @foreach (var imagen in imagenes)
                        {
                            <div class="col-md-4 mb-3">
                                <div class="card" style="cursor: pointer;" 
                                     @onclick="() => SeleccionarImagen(imagen)">
                                    <img src="api/upload/getimagenbyid?id=@imagen.Id" 
                                         class="card-img-top" style="height: 200px; object-fit: cover;" />
                                    <div class="card-body">
                                        <small class="text-muted">@imagen.FileName</small>
                                    </div>
                                </div>
                            </div>
                        }
                    </div>
                </div>
            </div>
        </div>
    }
}

@code {
    private bool mostrarModal = false;
    private List<UploadResult> imagenes = [];
    private EventCallback<UploadResult> OnSeleccionar { get; set; }

    private async Task AbrirModal()
    {
        mostrarModal = true;
        imagenes = await UploadService.GetAllImages();
    }

    private void CerrarModal()
    {
        mostrarModal = false;
    }

    private void SeleccionarImagen(UploadResult imagen)
    {
        CerrarModal();
        // Hacer algo con la imagen seleccionada...
    }
}
```

---

## 5️⃣ Servicio Extendido con Eliminación

```csharp
// WEB/Services/Upload/UploadService.cs - Método adicional

public async Task<bool> DeleteImageAsync(int id)
{
    var url = $"api/upload/deleteimage?id={id}";
    var clienteHttp = HttpFactory.CreateClient("ritrama");
    var response = await clienteHttp.DeleteAsync(url);
    return response.IsSuccessStatusCode;
}
```

```csharp
// API/Services/Upload/IUploadService.cs - Interfaz extendida

public interface IUploadService
{
    Task<List<UploadResult>> UploadFilesAsync(List<IFormFile> files);
    Task<List<UploadResult>> GetAllImagesAsync();
    Task<UploadResult?> GetImageByIdAsync(int id);
    Task<bool> DeleteImageAsync(int id);
}
```

```csharp
// API/Services/Upload/UploadService.cs - Implementación

public async Task<bool> DeleteImageAsync(int id)
{
    var upload = await _context.Uploads.FindAsync(id);
    if (upload == null) return false;

    try
    {
        var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", upload.StoredFileName!);
        if (File.Exists(uploadsPath))
            File.Delete(uploadsPath);

        _context.Uploads.Remove(upload);
        await _context.SaveChangesAsync();
        return true;
    }
    catch
    {
        return false;
    }
}
```

```csharp
// API/Controllers/UploadController.cs - Endpoint DELETE

[HttpDelete]
[Route("deleteimage")]
public async Task<IActionResult> DeleteImage(int id)
{
    var success = await _uploadService.DeleteImageAsync(id);
    return success ? Ok("Imagen eliminada") : NotFound("Imagen no encontrada");
}
```

---

## 6️⃣ Filtros y Búsqueda en Galería

```razor
@page "/GaleriaConFiltro"
@inject UploadService UploadService

<div class="mb-3">
    <input type="text" class="form-control" placeholder="Buscar..." 
           @oninput="@((ChangeEventArgs e) => FiltrarImagenes(e.Value?.ToString()))" />
</div>

<div class="row">
    @foreach (var imagen in imagenesFiltradas)
    {
        <div class="col-md-4 mb-3">
            <div class="card">
                <img src="api/upload/getimagenbyid?id=@imagen.Id" 
                     class="card-img-top" style="height: 200px; object-fit: cover;" />
                <div class="card-body">
                    <h6>@imagen.FileName</h6>
                    <small class="text-muted">@imagen.ContentType</small>
                </div>
            </div>
        </div>
    }
</div>

@code {
    private List<UploadResult> imagenes = [];
    private List<UploadResult> imagenesFiltradas = [];
    private string? filtro;

    protected override async Task OnInitializedAsync()
    {
        imagenes = await UploadService.GetAllImages();
        imagenesFiltradas = imagenes;
    }

    private void FiltrarImagenes(string? texto)
    {
        filtro = texto?.ToLower();
        imagenesFiltradas = string.IsNullOrEmpty(filtro)
            ? imagenes
            : imagenes.Where(i => 
                (i.FileName?.ToLower().Contains(filtro) ?? false)).ToList();
    }
}
```

---

## 7️⃣ Descarga de Imagen

```razor
@page "/DescargarImagen"
@inject UploadService UploadService

@foreach (var imagen in imagenes)
{
    <div class="card mb-2">
        <div class="card-body d-flex justify-content-between align-items-center">
            <span>@imagen.FileName</span>
            <button class="btn btn-sm btn-success" 
                    @onclick="() => DescargarImagen(imagen)">
                Descargar
            </button>
        </div>
    </div>
}

@code {
    private List<UploadResult> imagenes = [];

    protected override async Task OnInitializedAsync()
    {
        imagenes = await UploadService.GetAllImages();
    }

    private async Task DescargarImagen(UploadResult imagen)
    {
        try
        {
            var bytes = await UploadService.GetImageById(imagen.Id);

            // Crear blob y descargar
            await JS.InvokeVoidAsync("descargarArchivo", 
                bytes, imagen.FileName, imagen.ContentType);
        }
        catch (Exception ex)
        {
            // Manejar error
        }
    }
}
```

---

## 📝 Script JavaScript para Descarga

```javascript
// Agregaar en app.js o en _Layout.html

window.descargarArchivo = function(contenido, nombreArchivo, contentType) {
    const blob = new Blob([new Uint8Array(contenido)], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nombreArchivo;
    link.click();
    window.URL.revokeObjectURL(url);
};
```

---

**¡Estos ejemplos te darán ideas para extender funcionalmente! 🚀**
