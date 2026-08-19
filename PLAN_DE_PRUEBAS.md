# Plan de Pruebas

Proyecto: RitramaCloud2026 (Blazor WEB + API .NET)
Fecha: 2026-08-07
Alcance: cambios recientes (HEAD `d721fe0` - productos con agentes, integración móvil/órdenes de compra)

## Resumen de cambios a probar

### 1. Módulo Productos
- `UpdateProductAsync` guarda todos los campos: `Product_Code`, `Costo`, `Stock`, `Stock_Mix`, `Stock_Max`, `StockStatus`, `Description`, `Marca`, `Model`, `PartNumber`, `SkuNumber`, `StatusProducts`.
- `Costo` ahora es `decimal(18,2)` (migración `ChangeProductCostoToDecimal`).
- Importación Excel (`POST api/products/import-excel`): headers flexibles, upsert por lotes de 500 con transacción, resultado `Inserted/Updated/Skipped/Errors`.
- `BulkCreateProductsAsync` + `POST api/products/bulkcreateproducts`.
- Imágenes guardadas vía `ProductImageStorage.GetPath` (configurable).
- `getproductimage` con headers `no-cache` para evitar caché del navegador.

### 2. Config (nuevo)
- Tablas `Categories` y `ProductUnits` (autocreación si no existen).
- CRUD: `GET/POST categories`, `PUT/DELETE categories/{id}`; igual para `units`.
- Validaciones: nombre obligatorio y sin duplicados (case-insensitive).
- Página `/config` con gestión visual de categorías/unidades.

### 3. Órdenes de compra / integración móvil
- DTO `OrdenCompra` ampliado: `Tipo_Documento`, `Subtotal`, `Impuesto`, `Supply_Id`, `Supply_Name`, `Reference`.
- Migración `ChangeOrderShip`: columna `Tipo_Documento` en tabla `Compra`.
- `IOrdenCompraService`/`OrdenCompraService` (`GET api/ordencompra/getorders`).
- Página `/mobile`: tabla de documentos sincronizados, botón Sincronizar, modal de detalle.

## Casos de prueba

### Productos - CRUD
| # | Caso | Pasos | Resultado esperado |
|---|------|-------|--------------------|
| P1 | Crear producto válido | Nuevo > llenar código, nombre, categoría, unidad, costo, subir hasta 3 imágenes > Guardar | Se guarda; aparece en el grid con todos los campos |
| P2 | Código obligatorio | Crear sin `Product_Code` | Validación bloquea el guardado |
| P3 | Combos de categoría/unidad | Crear/editar y abrir selects | Solo listan ítems `IsActive` de Config |
| P4 | Costo decimal | Guardar costo `12.50` | Se muestra y persiste con 2 decimales |
| P5 | Límite imágenes | Intentar subir más de 3 imágenes | Solo se permiten 3 |
| P6 | Editar producto completo | Modificar stock, marca, SKU, descripción, etc. | Todos los campos se conservan al guardar |
| P7 | Refresco del grid | Guardar edición con nueva imagen | La fila se actualiza sin recargar; la imagen nueva se muestra (sin caché) |
| P8 | Error de guardado | Causar fallo en API | Aparece alerta de error en el modal, no se cierra la sesión |
| P9 | Eliminar producto | Eliminar con confirmación | El producto desaparece; el grid no se rompe |

### Importación Excel
| # | Caso | Pasos | Resultado esperado |
|---|------|-------|--------------------|
| I1 | Descargar plantilla | Clic en "Descargar Plantilla" | Se descarga `PlantillaProductos.xlsx` válido |
| I2 | Importar válido | Subir Excel con productos nuevos | Resumen: insertados > 0; la lista se recarga |
| I3 | Upsert | Reimportar el mismo archivo | Se actualizan los existentes, no se duplican |
| I4 | Fila sin código | Fila sin `product_code` | Fila omitida + error "código obligatorio" |
| I5 | Fila sin nombre | Fila sin `nombre` | Fila omitida + error "nombre obligatorio" |
| I6 | Costo inválido | Costo no numérico | Fila omitida + error de costo |
| I7 | Código duplicado en archivo | Dos filas con mismo código | Segunda fila omitida + error de duplicado |
| I8 | Encabezado faltante | Sin columna código o nombre | Importación falla con mensaje claro |
| I9 | Archivo no Excel | Subir `.txt`/`.pdf` (selector y drag&drop) | Mensaje "debe ser .xlsx o .xls" |
| I10 | Archivo vacío | Excel sin datos | Resultado sin errores, 0 insertados |
| I11 | Drag & drop | Arrastrar archivo sobre la zona | Se importa igual que con selector |
| I12 | Rollback | Forzar error a mitad de carga | No quedan productos a medias (transacción revierte) |
| I13 | Lote grande | Archivo con > 500 filas | Se procesa en lotes sin errores |

### Config - Categorías y Unidades
| # | Caso | Pasos | Resultado esperado |
|---|------|-------|--------------------|
| C1 | Crear categoría/unidad | Nueva > guardar | Aparece en la lista |
| C2 | Nombre duplicado | Crear con nombre existente (variando mayúsculas) | Rechazado con mensaje |
| C3 | Nombre vacío | Guardar sin nombre | Rechazado |
| C4 | Editar | Cambiar nombre/descripción/activo | Se actualiza |
| C5 | Eliminar | Borrar con confirmación | Desaparece de la lista |
| C6 | Búsqueda | Filtrar por nombre/descripción | Filtra correctamente |
| C7 | Combos en productos | Crear/editar producto | Solo categorías/unidades activas en el select |

### Imágenes
| # | Caso | Pasos | Resultado esperado |
|---|------|-------|--------------------|
| IM1 | Subir imagen en edición | Reemplazar imagen de producto | Se muestra la nueva (headers no-cache) |
| IM2 | Eliminar imagen | Eliminar imagen existente | Desaparece del grid y de disco |

### Órdenes de compra / integración móvil
| # | Caso | Pasos | Resultado esperado |
|---|------|-------|--------------------|
| O1 | Sincronizar | `/mobile` > botón Sincronizar | Carga la lista de órdenes |
| O2 | Detalles | Clic en "Details" | Modal muestra encabezado + ítems |
| O3 | Nuevos campos | Revisar filas | Se muestran `Tipo_Documento`, `Subtotal`, `Impuesto`, proveedor |
| O4 | Estado | Revisar columna Status/Sincro | Abierto/Cerrado y Sí/No correctos |

### Regresión general
| # | Caso | Pasos | Resultado esperado |
|---|------|-------|--------------------|
| R1 | Login | Entrar a la app | Autenticación funciona |
| R2 | Actualización de producto conserva campos | Editar y guardar | Descripción, marca, etc. ya NO se borran |
| R3 | Navegación | Recorrer módulos (products, config, mobile, etc.) | Sin errores de consola |

## Pendientes / observaciones
- Confirmar que las migraciones se aplicaron en la BD de pruebas (`ChangeProductCostoToDecimal`, `ChangeOrderShip`).
- Verificar config de `ProductImageStorage` (ruta de uploads) según entorno.
- `ModalUpdateOrdenCompra` es un placeholder (`<h3>`) - pendiente de implementar.
- En `ModalCreateProducts` el label dice "Coidgo de Producto" (typo menor).
- `FileEarmarkArrowUp`/`FileEarmarkCheck` dependen de que existan en la librería de iconos usada.
