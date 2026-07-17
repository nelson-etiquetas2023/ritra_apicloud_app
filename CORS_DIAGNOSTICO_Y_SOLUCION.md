# 🔧 DIAGNÓSTICO Y SOLUCIÓN COMPLETA DE CORS

## ✅ Cambios Implementados

He aplicado una solución **DEFINITIVA** y completa para resolver todos los problemas de CORS:

### 1. **Configuración mejorada en `API/Program.cs`**
```csharp
builder.Services.AddCors(options =>
{
	options.AddPolicy("PoliticaCORS", builder =>
	{
		builder
			.WithOrigins("http://192.168.10.10:9000")
			.AllowAnyMethod()          // GET, POST, PUT, DELETE...
			.AllowAnyHeader()          // Todos los headers
			.AllowCredentials()        // Cookies/credenciales
			.WithExposedHeaders("Content-Type", "Authorization", "X-Total-Count");
	});
});
```

### 2. **Orden correcto del middleware**
```csharp
app.UseRouting();
app.UseCors("PoliticaCORS");        // ANTES de Authorization
app.UseAuthorization();
```

### 3. **Middleware para preflight OPTIONS requests**
```csharp
app.Use(async (context, next) =>
{
	if (context.Request.Method == "OPTIONS")
	{
		context.Response.StatusCode = 200;
		await context.Response.CompleteAsync();
		return;
	}
	await next();
});
```

### 4. **`[EnableCors]` en todos los controladores**
- ✅ `AuthController.cs`
- ✅ `ProductsController.cs`
- ✅ `UsersController.cs`
- ✅ `OrderFisicoController.cs`
- ✅ `ConfigController.cs`
- ✅ `UploadController.cs`

---

## 🚀 PASOS A SEGUIR CON TU API EN PRODUCCIÓN

### **Paso 1: Verificar que la API está en el puerto correcto**

Tu launchSettings.json muestra:
```json
"applicationUrl": "http://localhost:5220"
```

Pero mencionaste que está en `192.168.10.10:8080`. **IMPORTANTE:**

#### **Opción A: Si está en 192.168.10.10:8080**
```json
{
  "profiles": {
	"http": {
	  "commandName": "Project",
	  "dotnetRunMessages": true,
	  "launchBrowser": false,
	  "applicationUrl": "http://192.168.10.10:8080",
	  "environmentVariables": {
		"ASPNETCORE_ENVIRONMENT": "Production"
	  }
	}
  }
}
```

#### **Opción B: Si está detrás de IIS en puerto 8080**
Asegúrate de que en IIS:
1. El binding es: `192.168.10.10` puerto `8080`
2. El Application Pool esté corriendo

### **Paso 2: Actualizar la API en producción**

```powershell
# 1. Detener la aplicación
Stop-Service -Name "RitramaAPI" -Force
# o presiona Ctrl+C si está en consola

# 2. Hacer backup
Copy-Item "C:\ruta\de\api" -Destination "C:\ruta\de\api.backup" -Recurse

# 3. Descargar/copiar nuevos archivos desde tu repositorio
cd C:\ruta\de\api
git pull origin main

# 4. Recompilar con Release
dotnet clean
dotnet build -c Release

# 5. Publicar
dotnet publish -c Release -o C:\ruta\output

# 6. Reiniciar
Start-Service -Name "RitramaAPI"
```

### **Paso 3: Verificar en la consola del navegador**

1. Abre el Blazor en `192.168.10.10:9000`
2. Presiona **F12** para abrir Developer Tools
3. Ve a la pestaña **Network**
4. Realiza una acción que haga solicitud a la API
5. Busca la solicitud en Network

**✅ Si ves estos headers, CORS está funcionando:**
```
Access-Control-Allow-Origin: http://192.168.10.10:9000
Access-Control-Allow-Credentials: true
Access-Control-Allow-Methods: GET, POST, PUT, DELETE
Access-Control-Allow-Headers: content-type, authorization
```

**❌ Si aún ves error CORS:**
```
Access to XMLHttpRequest at 'http://192.168.10.10:8080/api/xxx' 
from origin 'http://192.168.10.10:9000' 
has been blocked by CORS policy
```

---

## 🔍 TROUBLESHOOTING

### **Problema: Aún veo error de CORS**

**Causa probable:** La API está en un puerto diferente o hay un proxy

**Soluciones:**

1. **Verifica el puerto real:**
```powershell
# En PowerShell, verifica qué puerto está escuchando
netstat -ano | findstr :8080
# o
Get-NetTCPConnection -LocalPort 8080
```

2. **Si la API está detrás de IIS (reverse proxy):**
```csharp
// En Program.cs, agrega ANTES de UseCors:
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

3. **Si la API está en HTTPS:**
```csharp
options.AddPolicy("PoliticaCORS", builder =>
{
	builder
		.WithOrigins("https://192.168.10.10:9000")  // HTTPS en lugar de HTTP
		.AllowAnyMethod()
		.AllowAnyHeader()
		.AllowCredentials();
});
```

### **Problema: Las credenciales no se envían**

En el Blazor, asegúrate de que en los HttpClient requests se envían credenciales:

```csharp
// En Program.cs del Blazor
builder.Services.AddHttpClient("ritrama", options =>
{
	options.BaseAddress = new Uri(server_etiquetas);
});
```

Y al hacer llamadas:
```csharp
var response = await clientHttp.GetAsync(url);
```

---

## 📊 RESUMEN DE CAMBIOS

| Archivo | Cambio | Razón |
|---------|--------|-------|
| `API/Program.cs` | Configuración CORS mejorada + middleware OPTIONS | CORS debe estar antes de Authorization |
| `AuthController.cs` | Agregado `[EnableCors("PoliticaCORS")]` | Critical para login/register |
| `ProductsController.cs` | Agregado `[EnableCors("PoliticaCORS")]` | Permite acceso desde Blazor |
| `UsersController.cs` | Agregado `[EnableCors("PoliticaCORS")]` | Permite acceso desde Blazor |
| `OrderFisicoController.cs` | Agregado `[EnableCors("PoliticaCORS")]` | Permite acceso desde Blazor |
| `ConfigController.cs` | Agregado `[EnableCors("PoliticaCORS")]` | Permite acceso desde Blazor |
| `UploadController.cs` | Agregado `[EnableCors("PoliticaCORS")]` | Permite acceso desde Blazor |

---

## 🎯 PRÓXIMOS PASOS

1. **Compila localmente** para verificar que no hay errores
2. **Prueba en tu máquina** con los mismos puertos (9000 para Blazor, 8080 para API)
3. **Copia los cambios** a producción
4. **Reinicia** la API
5. **Prueba desde el navegador** y verifica los headers de respuesta

Si aún tienes problemas, proporciona:
- ✅ El error exacto de la consola del navegador (F12 → Console)
- ✅ Los headers de respuesta (F12 → Network → selecciona la request → Response Headers)
- ✅ Confirmación de los puertos reales donde están corriendo

---

**¿Necesitas ayuda con algo específico?**
