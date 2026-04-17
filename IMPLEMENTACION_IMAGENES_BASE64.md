# Solución: Guardar Imágenes de Productos en Base64 con Nombres Seguros

## Resumen de Cambios Implementados

Se ha implementado un sistema completo para guardar imágenes de productos en base64 en el servidor, con nombres seguros usando GUID, y actualizar la tabla de imágenes automáticamente.

## Archivos Creados

### 1. `Shared/Dtos/Base64ImageData.cs`
DTO para encapsular datos de imagen en base64:
```csharp
public class Base64ImageData
{
    public string Base64Data { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public int ImageIndex { get; set; }
}
```

### 2. `Shared/Dtos/CreateProductWithImagesRequest.cs`
DTO para enviar producto con imágenes juntas:
```csharp
public class CreateProductWithImagesRequest
{
    public Product Product { get; set; }
    public List<Base64ImageData> Images { get; set; }
}
```

## Archivos Modificados

### 1. **API/Services/Upload/IUploadService.cs**
- Agregado método: `SaveBase64ImageAsync()`
- Permite guardar imágenes desde datos base64

### 2. **API/Services/Upload/UploadService.cs**
- Implementado `SaveBase64ImageAsync()` que:
  - Decodifica base64
  - Genera nombre seguro con GUID
  - Guarda archivo en carpeta uploads
  - Registra en tabla Uploads

### 3. **API/Services/Products/IProductsService.cs**
- Agregado método: `CreateProductWithImagesAsync(CreateProductWithImagesRequest request)`

### 4. **API/Services/Products/ProductsService.cs**
- Implementado `CreateProductWithImagesAsync()` que:
  - Crea el producto
  - Procesa cada imagen en base64
  - Genera nombre seguro con GUID para cada imagen
  - Guarda archivos en disco
  - Actualiza tabla ProductImages con referencias
  - Maneja errores individuales por imagen

### 5. **API/Controllers/ProductsController.cs**
- Nuevo endpoint: `POST /api/products/createproductwithimages`
- Recibe request con producto e imágenes en base64
- Retorna producto creado con referencia de imágenes

### 6. **WEB/Services/Products/IProductsService.cs**
- Agregado método: `CreateProductWithImagesAsync(CreateProductWithImagesRequest request)`

### 7. **WEB/Services/Products/ProductsService.cs**
- Implementado `CreateProductWithImagesAsync()` que:
  - Serializa el request en JSON
  - Envía a endpoint `/api/products/createproductwithimages`
  - Retorna resultado de éxito/fallo

### 8. **WEB/Pages/Products/ModalCreateProducts.razor**
- Agregado `@using Shared.Dtos`
- Reemplazado método `CreateProducts()`:
  - Extrae base64 de previews
  - Prepara lista de `Base64ImageData`
  - Crea `CreateProductWithImagesRequest`
  - Llama a `CreateProductWithImagesAsync()`
  - Muestra mensaje de éxito/error

## Flujo de Funcionamiento

### En el Cliente (Blazor)
1. Usuario selecciona imágenes en el modal
2. Las imágenes se cargan como base64 en `imagePreviews`
3. Al guardar, se extraen los datos base64
4. Se crea `CreateProductWithImagesRequest` con producto e imágenes
5. Se envía a servidor con un único POST

### En el Servidor (API)
1. Controller recibe `CreateProductWithImagesRequest`
2. Service crea el producto
3. Para cada imagen base64:
   - Decodifica base64 a bytes
   - Genera nombre único con Guid (ej: `a1b2c3d4-e5f6-7890.jpg`)
   - Guarda archivo en carpeta `uploads`
   - Crea registro en tabla `ProductImages`
4. Retorna producto creado

## Ventajas de Esta Implementación

✅ **Seguridad**: Nombres de archivo usando GUID evitan exposición de nombres originales  
✅ **Integridad**: Base64 se valida y decodifica antes de guardar  
✅ **Eficiencia**: Una única llamada para crear producto + imágenes  
✅ **Resilencia**: Errores en una imagen no afectan el producto  
✅ **Automatización**: Tabla uploads se actualiza automáticamente  
✅ **Escalabilidad**: Mismo patrón se puede usar para múltiples imágenes

## Instrucciones para Ejecutar

1. **Reiniciar la aplicación** (debido a cambios en interfaces)
2. **Las imágenes se guardarán automáticamente** al crear un producto
3. **Los archivos estarán en**: `{API}/uploads/` con nombres tipo `a1b2c3d4-e5f6-7890.jpg`
4. **La tabla ProductImages** contendrá referencias con StoredFileName

## Ejemplo de Datos Guardados

**En disco:**
```
uploads/
├── a1b2c3d4-e5f6-7890.jpg
├── b2c3d4e5-f6g7-8901.png
└── c3d4e5f6-g7h8-9012.jpg
```

**En BD (ProductImages):**
| Id | ProductId | FileName | StoredFileName | ContentType | ImageIndex |
|-----|-----------|----------|-----------------|------------|-----------|
| 1 | 1 | image1.jpg | a1b2c3d4-e5f6-7890.jpg | image/jpeg | 0 |
| 2 | 1 | image2.png | b2c3d4e5-f6g7-8901.png | image/png | 1 |
