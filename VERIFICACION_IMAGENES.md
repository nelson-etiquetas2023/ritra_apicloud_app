# ⚡ Checklist Rápido - Imágenes Funcionando

## 🎯 Antes de Probar

### Paso 1: Verifica el Puerto de la API
En `WEB/Program.cs`, busca esta línea:
```csharp
var ritrama_local = "http://localhost:5220/";
```

**¿Es este el puerto donde ejecutas tu API?**

Si NO, actualiza con el puerto correcto. Por ejemplo:
```csharp
var ritrama_local = "https://localhost:7000/";  // Si usas HTTPS
var ritrama_local = "http://localhost:5000/";   // Si es otro puerto
```

---

### Paso 2: Ejecuta Ambas Aplicaciones

```powershell
# Terminal 1
cd C:\Programacion\RitramaCloud2026\API
dotnet run
# Espera: "Application started. Press Ctrl+C to shut down"

# Terminal 2
cd C:\Programacion\RitramaCloud2026\WEB
dotnet run
# Espera: "Application started at https://localhost:7052"
```

---

### Paso 3: Navega a la Página

```
https://localhost:7052/Upload
```

---

### Paso 4: Sube una Imagen

1. Haz clic en "Selecciona una o más imágenes"
2. Elige una imagen
3. Espera a que termine de cargar

---

### Paso 5: Verifica que se Muestra

Deberías ver la imagen en una tarjeta abajo.

**¿Se ve la imagen?**

- **SÍ** ✅ → ¡Problema resuelto!
- **NO** ❌ → Continúa con el troubleshooting

---

## 🔍 Troubleshooting - Si NO se Ve la Imagen

### 1. Abre DevTools (F12)

Presiona `F12` en el navegador.

### 2. Ve a la Tab "Network"

En DevTools, busca la pestaña "Network".

### 3. Sube una imagen

Vuelve a subir una imagen mientras tienes DevTools abierto.

### 4. Busca la petición

En Network, busca una petición que contenga `getimagenbyid`.

**¿Qué status code tiene?**

```
200 ✅ → El servidor retorna la imagen correctamente
       Posible problema: URL mal en HTML
       → Revisa que GetImageUrl() devuelve URL correcta

404 ❌ → El archivo no existe en el servidor
       Posible problema: 
       → API no está guardando archivos
       → Ruta de uploads incorrecta
       → BD no tiene registros

500 ❌ → Error en el servidor
       → Revisa logs en consola de API
```

---

## 🔧 Soluciones Rápidas

### Solución 1: Limpiar Caché
```
Ctrl + Shift + Delete
Selecciona: Todo
Clic: Borrar datos
```

Luego recarga la página: `F5`

### Solución 2: Forzar Refresh sin Caché
```
Ctrl + Shift + R  (Windows/Linux)
Cmd + Shift + R   (Mac)
```

### Solución 3: Verifica la URL en consola
En DevTools → Console, ejecuta:
```javascript
document.querySelectorAll('img').forEach(img => console.log(img.src));
```

Debería mostrar URLs como:
```
http://localhost:5220/api/upload/getimagenbyid?id=1
http://localhost:5220/api/upload/getimagenbyid?id=2
```

**¿Es correcto el puerto (5220)?**
- SÍ → Revisa API
- NO → Actualiza `Program.cs`

### Solución 4: Verifica que API está guardando
En PowerShell:
```powershell
Get-ChildItem "C:\Programacion\RitramaCloud2026\API\uploads\"
```

¿Hay archivos `.tmp`?
- SÍ → Bien
- NO → API no está guardando, revisa Program.cs

### Solución 5: Verifica BD
Ejecuta en SQL Server:
```sql
SELECT COUNT(*) as Total FROM Uploads;
```

¿Hay registros?
- SÍ (> 0) → Bien
- NO (0) → BD vacía, verifica API

---

## 📊 Matriz de Decisión

```
¿Se ve la imagen?
    │
    ├─ SÍ ✅
    │   └─ ¡Felicidades! Problema resuelto
    │
    └─ NO ❌
        │
        ├─ DevTools Network = 200
        │   └─ URL en HTML ¿correcta?
        │       ├─ SÍ → Problema en navegador
        │       │   └─ Limpia caché (Ctrl+Shift+Del)
        │       └─ NO → Actualiza GetImageUrl()
        │
        ├─ DevTools Network = 404
        │   └─ ¿Archivo existe en API/uploads/?
        │       ├─ SÍ → Ruta en API incorrecta
        │       └─ NO → API no está guardando
        │
        └─ DevTools Network = Error/No aparece
            └─ ¿API está ejecutándose?
                ├─ SÍ → CORS problem
                │   └─ Revisa Program.cs (API)
                └─ NO → Ejecuta API primero
```

---

## ✅ Verificación Final

Cuando todo funcione, deberías ver:

```
┌─────────────────────────────────────────┐
│  🖼️  Gestor de Imágenes                 │
├─────────────────────────────────────────┤
│                                         │
│  📤 Cargar Nuevas Imágenes              │
│  [Selecciona imagen]                    │
│                                         │
│  🗂️  Imágenes Guardadas (1)             │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │ [IMAGEN VISIBLE]                 │  │
│  │ Nombre: foto.jpg                 │  │
│  │ Almacenado: a1b2c3d4e5f6.tmp     │  │
│  │ ID: 1                            │  │
│  └──────────────────────────────────┘  │
│                                         │
└─────────────────────────────────────────┘
```

---

## 📝 Resumen de los Cambios

### Archivo: `WEB/Pages/Upload/Index.razor`

**Cambio 1:** Agregar inyección
```csharp
@inject IHttpClientFactory HttpClientFactory
```

**Cambio 2:** Usar URL completa en img
```html
<!-- Antes -->
<img src="api/upload/getimagenbyid?id=@image.Id" />

<!-- Ahora -->
<img src="@GetImageUrl(image.Id)" />
```

**Cambio 3:** Agregar método en @code
```csharp
private string GetImageUrl(int imageId)
{
    var client = HttpClientFactory.CreateClient("ritrama");
    var baseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5220";
    return $"{baseUrl}/api/upload/getimagenbyid?id={imageId}";
}
```

---

## 🎯 Resultado Esperado

| Acción | Antes | Después |
|--------|-------|---------|
| Cargar imagen | ✅ Sube OK | ✅ Sube OK |
| Ver en galería | ❌ Imagen rota | ✅ Imagen visible |
| URL en src | `api/...` (relativa) | `http://localhost:5220/api/...` (absoluta) |

---

## 🚀 Siguientes Pasos

1. ✅ Implementa los cambios (ya hechos)
2. ✅ Ejecuta ambas apps
3. ✅ Prueba subir imagen
4. ✅ Verifica que se muestra

Si todo funciona → **¡Listo!** 🎉

Si sigue fallando → Ejecuta el troubleshooting anterior

---

**¿Preguntas? Revisa el archivo: `SOLUCION_IMAGENES_ROTAS.md`**
