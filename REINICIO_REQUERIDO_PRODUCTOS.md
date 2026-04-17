# ⚠️ REINICIO REQUERIDO - Módulo de Productos con Imágenes

## 🔴 Estado Actual

Se realizaron cambios que requieren **reinicio completo** de ambas aplicaciones.

### Por qué?

- Cambios en interfaces (IProductsService)
- Cambios en visibilidad de propiedades (Product.Images)
- Hot reload no puede aplicar estos cambios

---

## 🚀 Pasos para Reiniciar

### Paso 1: Detener API (si está ejecutándose)

En la terminal de API:
```powershell
Ctrl + C
```

Espera a que se cierre completamente.

### Paso 2: Detener WEB (si está ejecutándose)

En la terminal de WEB:
```powershell
Ctrl + C
```

Espera a que se cierre completamente.

### Paso 3: Ejecutar API

Abre una terminal PowerShell:

```powershell
cd C:\Programacion\RitramaCloud2026\API
dotnet run
```

Espera a ver:
```
Application started. Press Ctrl+C to shut down.
```

### Paso 4: Ejecutar WEB (nueva terminal)

Abre otra terminal PowerShell:

```powershell
cd C:\Programacion\RitramaCloud2026\WEB
dotnet run
```

Espera a ver:
```
Application started at https://localhost:7052
```

### Paso 5: Abre el navegador

```
https://localhost:7052/
```

---

## ✅ Verificación Post-Reinicio

### Test 1: Crear Producto

1. Navega a **Productos**
2. Haz clic en **➕ Crear Producto**
3. ¿Se abre modal? ✅

### Test 2: Ver Imágenes en Modal

1. En el modal, desplázate hasta abajo
2. ¿Ves sección "📸 Imágenes del Producto (máx 3)"? ✅
3. ¿Ves 3 áreas con upload? ✅

### Test 3: Subir Imagen

1. Haz clic en primer "Selecciona archivo"
2. Selecciona una imagen
3. ¿Se muestra preview? ✅

### Test 4: Guardar Producto

1. Ingresa datos del producto
2. Selecciona 1-3 imágenes
3. Haz clic **Guardar**
4. ¿Se cierra modal? ✅
5. ¿Aparece en lista? ✅

### Test 5: Ver Imagen en QuickGrid

1. En la lista de productos
2. ¿Aparece columna "Imagen"? ✅
3. ¿Muestra primera imagen? ✅
4. ¿Los nuevos productos muestran imagen? ✅

---

## 🐛 Si Algo Sale Mal

### Error: "Aplicación no inicia"

```powershell
# Intenta limpiar y reconstruir
dotnet clean
dotnet build
dotnet run
```

### Error: "Puerto ya en uso"

```powershell
# API: cambiar puerto
# En API/Program.cs buscar puerto 5220

# WEB: cambiar puerto
# En WEB/Program.cs buscar puerto 7052

# O matar procesos:
Get-Process | Where-Object {$_.ProcessName -eq "dotnet"} | Stop-Process
```

### Error: "Base de datos"

```powershell
# Crear migrations si es necesario
cd API
dotnet ef migrations add AddProductImages
dotnet ef database update
```

### Error en consola del navegador (F12)

```
CORS error → API no tiene CORS habilitado
404 en imagen → API no está ejecutándose
Conexión rechazada → Verificar puerto en Program.cs
```

---

## 📋 Cambios Realizados

### Modelos (Shared)
- ✅ `Product.cs` - Images ahora es público
- ✅ `ProductImage.cs` - Agregó relación con Product

### Servicios (API)
- ✅ `IProductsService` - Nuevos métodos
- ✅ `ProductsService` - Lógica de imágenes

### Servicios (WEB)
- ✅ `IProductsService` - Nuevos métodos
- ✅ `ProductsService` - Integración con API

### UI (WEB)
- ✅ `ModalCreateProducts.razor` - Sección de imágenes
- ✅ `ModalCreateProducts.razor.css` - Estilos
- ✅ `QuickGridProducts.razor` - Columna de imagen
- ✅ `QuickGridProducts.razor.css` - Estilos thumbnail

### API Controllers
- ✅ `ProductsController.cs` - Endpoints de imágenes

---

## 📊 Qué Esperar

### Después del reinicio:

1. **Modal Crear Producto** tendrá sección de imágenes
2. **Subir hasta 3 imágenes** por producto
3. **Ver previews** mientras seleccionas
4. **Guardar producto** crea carpeta de imágenes
5. **QuickGrid** muestra primera imagen
6. **Eliminar producto** elimina sus imágenes

---

## ⏱️ Tiempo Estimado

- Detener apps: **2 min**
- Ejecutar API: **1-2 min**
- Ejecutar WEB: **1-2 min**
- Verificación: **5 min**

**Total: ~10-15 minutos**

---

## 🎯 Próximos Pasos Después del Reinicio

1. ✅ Crear un producto de prueba con 3 imágenes
2. ✅ Verificar que se guardan en `/uploads`
3. ✅ Verificar que se ven en QuickGrid
4. ✅ Probar eliminar producto (se eliminan imágenes)
5. ✅ Probar crear producto sin imágenes

---

## 📞 Solución Rápida

Si todo falla:

```powershell
# Limpiar caché
Remove-Item -Recurse -Force bin/
Remove-Item -Recurse -Force obj/

# Reconstruir
dotnet clean
dotnet build

# Ejecutar
dotnet run
```

---

**¡Reinicia las aplicaciones y disfruta de la nueva funcionalidad!** 🚀

El sistema de productos con imágenes estará completamente operacional.
