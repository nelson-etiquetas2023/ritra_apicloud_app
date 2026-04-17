# ✅ Implementación: Edición de Imágenes de Productos

## 🎯 Objetivo
Agregar funcionalidad para que el usuario pueda cambiar las imágenes de un producto existente desde el modal de edición (que se abre al hacer clic en el icono del lápiz en el QuickGrid).

---

## 📋 Cambios Implementados

### **1. Frontend - ModalUpdateProducts.razor**

#### **Nuevas Características:**
- ✅ Carga automática de imágenes existentes del producto
- ✅ Vista previa de las 3 imágenes del producto
- ✅ Botón "Cambiar" en cada imagen
- ✅ Capacidad de reemplazar cualquier imagen
- ✅ Eliminación de imágenes antes de guardar

#### **Estructura del Modal:**
```
┌─────────────────────────────────────────┐
│ Modificar Producto                      │
├─────────────────────────────────────────┤
│ [Nombre]  [Categoría]  [Unidad]         │
│ [Precio]  [Código de Barra]             │
│ [Desactivar]                            │
│                                         │
│ 📸 Imágenes del Producto                │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐ │
│ │ Imagen 1 │ │ Imagen 2 │ │ Imagen 3 │ │
│ │  [✓]     │ │  [✓]     │ │  [✓]     │ │
│ │ [Cambiar]│ │ [Cambiar]│ │ [Cambiar]│ │
│ └──────────┘ └──────────┘ └──────────┘ │
│                                         │
│ [Guardar] [Cancelar]                    │
└─────────────────────────────────────────┘
```

#### **Métodos Clave:**

```csharp
// Cargar imágenes existentes del producto
private async Task LoadProductImages()
{
    // Lee las imágenes de la BD y las muestra en preview
    // Si están disponibles, crea base64 para visualizarlas
}

// Abre el diálogo de selección de archivos
private async Task OpenFileDialog(int index)
{
    // Permite seleccionar archivo para índice (0, 1 o 2)
}

// Procesa la imagen seleccionada
private async Task OnFileSelected(InputFileChangeEventArgs e)
{
    // Convierte a base64
    // Agrega a lista de nuevas imágenes (newImages)
    // Actualiza preview visual
}

// Elimina imagen antes de guardar
private void RemoveImage(int index)
{
    // Limpia preview e información
    // Remueve de lista de nuevas imágenes
}

// Guarda producto + nuevas imágenes
private async Task UpdateProducts()
{
    // Actualiza datos del producto
    // Actualiza cada imagen nueva llamando al servicio
}
```

#### **Variables de Estado:**
```csharp
private string[] imagePreviews = new string[3];      // Previews en base64
private string[] imageNames = new string[3];         // Nombres de archivos
private List<Base64ImageData> newImages = new();     // Imágenes nuevas a guardar
private int nextImageSlot = 0;                       // Slot actual
```

---

### **2. Frontend - ModalUpdateProducts.razor.css**

Nuevo archivo con estilos para:
- ✅ Cajas de edición de imagen (image-edit-box)
- ✅ Previsualizaciones (image-preview-area)
- ✅ Placeholders para imágenes vacías
- ✅ Botones de acción (Cambiar, Eliminar)
- ✅ Efectos hover y transiciones
- ✅ Diseño responsivo para móvil

**Características principales:**
- Imágenes de 200x200px en editor
- `object-fit: contain` para calidad completa
- Bordes redondeados y sombras
- Botón de eliminar con icono ✕
- Botón "Cambiar" debajo de cada imagen

---

### **3. Servicios Cliente**

#### **IProductsService.cs - Agregar método:**
```csharp
Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData);
```

#### **ProductsService.cs - Implementar método:**
```csharp
public async Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData)
{
    // Envía PUT request a: /api/products/updateproductimage/{productId}
    // Serializa Base64ImageData a JSON
    // Retorna éxito/fallo
}
```

---

### **4. Servicios Servidor**

#### **IProductsService.cs (API) - Agregar método:**
```csharp
Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData);
```

#### **ProductsService.cs (API) - Implementar método:**

```csharp
public async Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData)
{
    // 1. Busca producto con sus imágenes
    // 2. Decodifica base64 a bytes
    // 3. Genera nombre seguro con Guid
    // 4. Busca imagen existente en ImageIndex
    // 5. Si existe:
    //    - Elimina archivo antiguo del disco
    //    - Actualiza referencia en BD
    // 6. Si no existe:
    //    - Crea nueva ProductImage
    // 7. Guarda archivo en /uploads
    // 8. Guarda cambios en BD
}
```

**Lógica de Actualización:**
```
Usuario selecciona nueva imagen para slot 0
    ↓
OnFileSelected() → Agrega a newImages[0]
    ↓
UpdateProducts() llamado
    ↓
UpdateProductImageAsync(productId, newImages[0])
    ↓
API busca ProductImage con ImageIndex=0
    ↓
Si existe:
  - Elimina archivo antiguo
  - Actualiza StoredFileName
Si no existe:
  - Crea nuevo ProductImage
    ↓
Guarda archivo nuevo en /uploads/GUID.jpg
    ↓
Guarda BD
    ↓
Actualiza QuickGrid
```

---

### **5. API Controller**

#### **ProductsController.cs - Nuevo Endpoint:**

```csharp
[HttpPut]
[Route("updateproductimage/{productId}")]
public async Task<IActionResult> UpdateProductImageAsync(int productId, [FromBody] Base64ImageData imageData)
{
    // Recibe: PUT /api/products/updateproductimage/1
    // Body: { base64Data, fileName, contentType, imageIndex }
    // Retorna: 200 OK o 500 Error
}
```

---

## 🔄 Flujo Completo de Uso

### **Paso 1: Abrir Modal de Edición**
```
Usuario hace clic en icono 🖊️ (lápiz) en QuickGrid
    ↓
ShowAsync() es llamado en ModalUpdateProducts
    ↓
LoadProductImages() carga las 3 imágenes existentes
    ↓
imagePreviews[] se llena con base64
    ↓
Modal se muestra con previsualizaciones
```

### **Paso 2: Cambiar Imagen**
```
Usuario hace clic en botón "Cambiar" de imagen 1
    ↓
OpenFileDialog(1) abre explorador de archivos
    ↓
Usuario selecciona nueva imagen
    ↓
OnFileSelected() convierte a base64
    ↓
imagePreviews[1] actualiza visualmente
    ↓
newImages.Add({ base64, fileName, contentType, 1 })
```

### **Paso 3: Guardar Cambios**
```
Usuario hace clic en "Guardar"
    ↓
UpdateProducts() inicia PreloadService
    ↓
UpdateProductAsync() actualiza datos del producto
    ↓
Para cada newImages[i]:
    UpdateProductImageAsync(productId, newImages[i])
        ↓
        API elimina archivo antiguo (si existe)
        ↓
        API guarda archivo nuevo con Guid
        ↓
        API actualiza BD
    ↓
Modal se cierra
    ↓
QuickGrid se actualiza
```

---

## 📊 Estructura de Datos

### **Base64ImageData (DTO existente):**
```csharp
public class Base64ImageData
{
    public string Base64Data { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public int ImageIndex { get; set; }  // 0, 1, 2
}
```

### **ProductImage en BD:**
```
Id | ProductId | ImageIndex | FileName | StoredFileName | ContentType
1  | 5         | 0          | img1.jpg | a1b2c3d4-e.jpg | image/jpeg
2  | 5         | 1          | img2.jpg | b2c3d4e5-f.jpg | image/jpeg
3  | 5         | 2          | img3.jpg | c3d4e5f6-g.jpg | image/jpeg
```

---

## 🎨 Interfaz Visual

### **Imagen Existente:**
```
┌─────────────────────┐
│                     │
│    [Imagen visible] │
│          ✕ (btn)    │  ← Botón para eliminar
│                     │
├─────────────────────┤
│  ✓ Imagen 1         │  ← Indicador de éxito
├─────────────────────┤
│   [Cambiar]         │  ← Botón para reemplazar
└─────────────────────┘
```

### **Imagen Vacía:**
```
┌─────────────────────┐
│         📷          │
│                     │
│     Imagen 2        │
│  Haz clic para      │
│     cambiar         │
├─────────────────────┤
│   [Cambiar]         │
└─────────────────────┘
```

---

## ⚙️ Tecnologías Utilizadas

- ✅ **Blazor WebAssembly** - Interfaz del usuario
- ✅ **ASP.NET Core API** - Backend
- ✅ **Base64** - Codificación de imágenes
- ✅ **Guid** - Nombres seguros de archivos
- ✅ **Entity Framework** - Acceso a BD
- ✅ **Bootstrap** - Estilos (CSS)

---

## 🚀 Próximos Pasos

1. **Reinicia la aplicación** (cambios en interfaces)
2. **Navega a /products**
3. **Haz clic en icono 🖊️ para editar producto**
4. Las imágenes se cargarán automáticamente
5. **Haz clic en "Cambiar" para reemplazar cualquier imagen**
6. **Guarda los cambios**

---

## 📝 Notas Importantes

- ✅ Las imágenes antiguas se eliminan automáticamente del disco
- ✅ Los nombres de archivo son seguros (GUID)
- ✅ Solo se actualiza la BD si la operación es exitosa
- ✅ El usuario puede cambiar 0, 1, 2 o 3 imágenes
- ✅ El LoadingIndicator muestra el progreso
- ✅ Los errores se registran en consola para debugging

---

## 🔍 Archivos Modificados/Creados

### Modificados:
- ✅ `WEB/Pages/Products/ModalUpdateProducts.razor`
- ✅ `WEB/Services/Products/IProductsService.cs`
- ✅ `WEB/Services/Products/ProductsService.cs`
- ✅ `API/Services/Products/IProductsService.cs`
- ✅ `API/Services/Products/ProductsService.cs`
- ✅ `API/Controllers/ProductsController.cs`

### Creados:
- ✅ `WEB/Pages/Products/ModalUpdateProducts.razor.css`

---

## 🎓 Resumen Técnico

| Aspecto | Detalle |
|--------|---------|
| **Flujo HTTP** | GET imágenes → PUT actualizar → POST crear |
| **Codificación** | Base64 para transferencia segura |
| **Almacenamiento** | /uploads/ con nombres Guid |
| **BD** | Tabla ProductImages con ImageIndex |
| **Limpieza** | Archivos antiguos eliminados automáticamente |
| **Transacciones** | Una BD save al final (atomicidad) |
| **Errores** | Manejados gracefully, no bloquean UI |
