# 🔧 Solución - Imágenes Rotas en Galería

## ✅ Problema Resuelto

Las imágenes no se mostraban porque la URL relativa `api/upload/getimagenbyid?id={id}` no funciona correctamente en Blazor WebAssembly.

## 🎯 Cambios Realizados

### 1. **Index.razor actualizado**
- ✅ Ahora inyecta `IHttpClientFactory`
- ✅ Usa método `GetImageUrl()` para construir URL completa
- ✅ Agrega fallback SVG si la imagen no carga

### 2. **Método `GetImageUrl()` Implementado**
```csharp
private string GetImageUrl(int imageId)
{
    // Construir URL completa para la API
    var client = HttpClientFactory.CreateClient("ritrama");
    var baseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5220";
    return $"{baseUrl}/api/upload/getimagenbyid?id={imageId}";
}
```

### 3. **Imagen con Fallback**
```html
<img src="@GetImageUrl(image.Id)" 
     alt="@image.FileName"
     loading="lazy"
     onerror="this.src='data:image/svg+xml,...'" />
```

---

## ⚡ Requisitos Importantes

### Verifica que tu API está en el puerto correcto

En `WEB/Program.cs`, revisa esta línea:
```csharp
var ritrama_local = "http://localhost:5220/";
```

**Si tu API está en diferente puerto, actualiza según corresponda:**

```csharp
// Si usas HTTPS en desarrollo:
var ritrama_local = "https://localhost:7000/";

// O tu puerto específico:
var ritrama_local = "http://localhost:TU_PUERTO/";
```

---

## 🔍 Cómo Verificar que Funciona

### 1. Ejecutar la Aplicación
```powershell
# Terminal 1 - API
cd API
dotnet run

# Terminal 2 - Blazor
cd WEB
dotnet run
```

### 2. Abrir DevTools (F12)
- Ir a **Network** tab
- Cargar una imagen nueva
- En Network, verifica que la request a `getimagenbyid` retorna status 200

### 3. Ver URL Completa
- Abre consola del navegador (F12 → Console)
- Ejecuta:
```javascript
document.querySelectorAll('img').forEach(img => console.log(img.src));
```

Deberías ver URLs como:
```
http://localhost:5220/api/upload/getimagenbyid?id=1
http://localhost:5220/api/upload/getimagenbyid?id=2
```

---

## 🐛 Si Aún Hay Problemas

### 1. Verifica que la API está corriendo
```bash
curl http://localhost:5220/api/upload/getimages
# Debería retornar JSON con la lista de imágenes
```

### 2. Verifica que el archivo existe en disco
```bash
# En PowerShell
Get-ChildItem "API\uploads\"

# Debería listar los archivos *.tmp
```

### 3. Verifica que la BD tiene registros
```sql
SELECT * FROM Uploads;
```

### 4. Limpia caché del navegador
```
Ctrl + Shift + Delete
```

### 5. Revisa logs en consola de navegador
- F12 → Console
- Busca mensajes de error

---

## 📡 Flujo Correcto

```
1. Imagen en HTML: <img src="http://localhost:5220/api/upload/getimagenbyid?id=1" />
   ↓
2. Navegador hace GET a http://localhost:5220/api/upload/getimagenbyid?id=1
   ↓
3. API recibe petición, busca en BD: SELECT * FROM Uploads WHERE Id=1
   ↓
4. Lee archivo: API/uploads/{StoredFileName}
   ↓
5. Retorna bytes de imagen con header Content-Type: image/jpeg
   ↓
6. Navegador renderiza imagen en pantalla ✅
```

---

## ✨ Caracteres Importantes del Fix

| Aspecto | Antes | Después |
|--------|-------|---------|
| URL en src | `api/upload/...` (relativa) | `http://localhost:5220/api/upload/...` (absoluta) |
| Constructor | Solo UploadService | + IHttpClientFactory |
| Método | N/A | `GetImageUrl()` |
| Fallback | Imagen rota ❌ | SVG placeholder ✅ |
| Error handling | No | Sí, con onerror |

---

## 🎉 Resultado

Ahora deberías ver:
- ✅ Imágenes cargadas correctamente en la galería
- ✅ Sin imágenes rotas
- ✅ Fallback SVG si algo falla
- ✅ URLs completas en network tab

---

## 📝 Código Actualizado (Resumen)

```razor
@page "/Upload" 
@using System.Net.Http.Headers
@inject UploadService Uploadservice
@inject IHttpClientFactory HttpClientFactory

<!-- Antes -->
<img src="api/upload/getimagenbyid?id=@image.Id" />

<!-- Ahora -->
<img src="@GetImageUrl(image.Id)" 
     alt="@image.FileName"
     loading="lazy"
     onerror="this.src='data:image/svg+xml,...'" />

@code {
    private string GetImageUrl(int imageId)
    {
        var client = HttpClientFactory.CreateClient("ritrama");
        var baseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5220";
        return $"{baseUrl}/api/upload/getimagenbyid?id={imageId}";
    }
}
```

---

## 🚀 Próximos Pasos

1. **Ejecuta la app** (Ctrl+F5 en VS)
2. **Sube una imagen** en /Upload
3. **Verifica que se muestra** en la galería ✅
4. **Abre F12 → Network** y confirma URLs correctas

---

**¡Problema resuelto! Las imágenes ahora se cargarán correctamente.** 🎉

Si sigue habiendo problemas, verifica:
- ✓ Puerto correcto en Program.cs
- ✓ API ejecutándose
- ✓ Archivos en `API/uploads/`
- ✓ Registros en tabla `Uploads`
