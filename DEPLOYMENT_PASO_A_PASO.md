# 🚀 GUÍA DE DEPLOYMENT PASO A PASO

## ⚠️ IMPORTANTE ANTES DE EMPEZAR

Verifica que tienes:
- ✅ Visual Studio o .NET 10 SDK instalado
- ✅ Acceso a la carpeta de aplicaciones en producción
- ✅ Acceso a la BD SQL Server
- ✅ PowerShell con permisos administrativos (si es servicio Windows)

---

## 📋 PRE-DEPLOYMENT CHECKLIST

```powershell
# 1. Verificar que el código compila
cd C:\Programacion\RitramaCloud2026
dotnet clean
dotnet build -c Release
# Debe terminar sin errores

# 2. Verificar puerto 8080
netstat -ano | findstr :8080
# Deberías ver LISTENING

# 3. Verificar conectividad a BD
sqlcmd -S SERVER-ETIQUETAS -N -U Npino -P "Jossycar5%" -Q "SELECT DB_NAME();"
# Debe retornar: RITRAMACLOUD

# 4. Verificar que el Blazor está en 9000
netstat -ano | findstr :9000
# Deberías ver LISTENING
```

Si alguno falla, no continúes - resuelve el problema primero.

---

## 🔧 DEPLOYMENT A PRODUCCIÓN

### **Opción A: Si la API está como Servicio Windows**

#### Paso 1: Detener el servicio
```powershell
# Con PowerShell como Administrador
Stop-Service -Name "RitramaAPI" -Force

# Esperar 3 segundos
Start-Sleep -Seconds 3

# Verificar que se detuvo
Get-Service -Name "RitramaAPI" | Select-Object Status
# Debe mostrar: Status Stopped
```

#### Paso 2: Hacer backup
```powershell
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
Copy-Item "C:\Ruta\Produccion\API" -Destination "C:\Backups\API_$timestamp" -Recurse
Write-Host "✅ Backup creado en: C:\Backups\API_$timestamp"
```

#### Paso 3: Compilar en Release
```powershell
cd C:\Programacion\RitramaCloud2026
dotnet clean -c Release
dotnet build -c Release
dotnet publish -c Release -o C:\Temp\API_Release

# Verificar que salió bien
if (Test-Path "C:\Temp\API_Release\API.dll") {
	Write-Host "✅ Publicación exitosa"
} else {
	Write-Host "❌ Error en la publicación"
}
```

#### Paso 4: Copiar archivos
```powershell
# Limpiar la carpeta de producción
Remove-Item "C:\Ruta\Produccion\API\*" -Recurse -Force

# Copiar nuevos archivos
Copy-Item "C:\Temp\API_Release\*" -Destination "C:\Ruta\Produccion\API\" -Recurse

Write-Host "✅ Archivos copiados exitosamente"
```

#### Paso 5: Iniciar el servicio
```powershell
Start-Service -Name "RitramaAPI"

# Esperar a que inicie
Start-Sleep -Seconds 5

# Verificar estado
Get-Service -Name "RitramaAPI" | Select-Object Status
# Debe mostrar: Status Running

Get-Service -Name "RitramaAPI" | Select-Object Status, DisplayName
```

#### Paso 6: Verificar puerto
```powershell
# Esperar 3 segundos más para que esté listo
Start-Sleep -Seconds 3

# Verificar puerto
$connection = Get-NetTCPConnection -LocalPort 8080 -ErrorAction SilentlyContinue
if ($connection) {
	Write-Host "✅ Puerto 8080 está en LISTENING"
	Write-Host "   Estado: $($connection.State)"
} else {
	Write-Host "❌ Puerto 8080 no está escuchando"
	Write-Host "   Verifica los logs del servicio"
}
```

---

### **Opción B: Si la API corre en Consola (dotnet run)**

#### Paso 1: Detener el proceso
```powershell
# Encontrar el proceso
$process = Get-Process | Where-Object { $_.ProcessName -like "*API*" }

if ($process) {
	Stop-Process -InputObject $process -Force
	Write-Host "✅ Proceso detenido"
} else {
	Write-Host "⚠️  No se encontró proceso de API"
}

# Esperar
Start-Sleep -Seconds 2
```

#### Paso 2: Hacer backup
```powershell
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
Copy-Item "C:\Ruta\Produccion\API" -Destination "C:\Backups\API_$timestamp" -Recurse
```

#### Paso 3: Compilar
```powershell
cd C:\Programacion\RitramaCloud2026
dotnet clean -c Release
dotnet build -c Release
```

#### Paso 4: Copiar archivos publicados
```powershell
dotnet publish -c Release -o C:\Ruta\Produccion\API
```

#### Paso 5: Iniciar la API nuevamente
```powershell
# En una nueva ventana de PowerShell como Administrador
cd C:\Ruta\Produccion\API
dotnet API.dll

# Verás logs como:
# info: Microsoft.Hosting.Lifetime[14]
# Application started.
# Listening on: http://0.0.0.0:8080
```

#### Paso 6: Verificar en otra ventana
```powershell
# En una NUEVA ventana de PowerShell
netstat -ano | findstr :8080
# Debe mostrar: LISTENING
```

---

## ✅ POST-DEPLOYMENT VERIFICATION

### **Test 1: Verificar que la API está respondiendo**
```powershell
try {
	$response = Invoke-WebRequest -Uri "http://192.168.10.10:8080/api/products/getproducts" `
		-Headers @{"Origin"="http://192.168.10.10:9000"} `
		-UseBasicParsing `
		-TimeoutSec 5

	Write-Host "✅ API está respondiendo (Status: $($response.StatusCode))"
} catch {
	Write-Host "❌ Error: $($_.Exception.Message)"
	Write-Host "   La API podría estar en error 500.30"
}
```

### **Test 2: Verificar headers CORS**
```powershell
try {
	$response = Invoke-WebRequest -Uri "http://192.168.10.10:8080/api/products/getproducts" `
		-Headers @{"Origin"="http://192.168.10.10:9000"} `
		-UseBasicParsing `
		-TimeoutSec 5

	Write-Host "✅ Headers CORS en respuesta:"
	$response.Headers.Keys | Where-Object { $_ -like "Access-Control*" } | ForEach-Object {
		Write-Host "   $($_): $($response.Headers[$_])"
	}
} catch {
	Write-Host "❌ Error al obtener headers"
}
```

### **Test 3: Probar desde el navegador**
1. Abre `http://192.168.10.10:9000` en el navegador
2. Presiona **F12** para abrir Developer Tools
3. Ve a la pestaña **Network**
4. Realiza una acción que llame a la API (ej: cargar productos)
5. Busca la solicitud en Network
6. Verifica los **Response Headers**

**Debe ver:**
```
Access-Control-Allow-Credentials: true
Access-Control-Allow-Origin: http://192.168.10.10:9000
Access-Control-Expose-Headers: Content-Type, Authorization, X-Total-Count
```

### **Test 4: Probar operaciones críticas**
- [ ] Login desde Blazor
- [ ] Cargar lista de productos
- [ ] Crear producto
- [ ] Actualizar producto
- [ ] Eliminar producto

---

## ❌ TROUBLESHOOTING SI FALLA

### **Problema: API no inicia (Error 500.30)**

```powershell
# Si es servicio Windows, ver logs en Event Viewer
Get-EventLog -LogName "Application" -Source ".NET Runtime" -Newest 10 | 
	Select-Object TimeGenerated, Message | 
	Format-Table -AutoSize

# Si es consola, ejecutar directamente y ver el error
cd C:\Ruta\Produccion\API
dotnet API.dll

# Buscar el error específico en la consola
```

**Causas comunes:**
- BD no accesible: Verifica `sqlcmd -S SERVER-ETIQUETAS ...`
- Credenciales incorrectas: Verifica `appsettings.json`
- Archivo no encontrado: Comprueba que copiaste todos los archivos

### **Problema: CORS aún bloqueando**

```powershell
# 1. Verificar que el puerto es exacto
# En navegador: http://192.168.10.10:9000 (NO localhost, NO 127.0.0.1)

# 2. Verificar que la API está en 8080
netstat -ano | findstr :8080

# 3. Limpiar cache del navegador
# F12 → Application → Clear site data

# 4. Probar en incógnita
# Ctrl+Shift+N (Chrome) o Ctrl+Shift+P (Firefox)
```

### **Problema: Servicio no inicia**

```powershell
# Ver estado detallado
Get-Service -Name "RitramaAPI" | Format-List *

# Ver último error
$service = Get-Service -Name "RitramaAPI"
$wmiService = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $service.Name }
Write-Output $wmiService.PathName

# Probar ejecutar manualmente el ejecutable
# para ver el error

# Reinstalar el servicio si es necesario
sc delete RitramaAPI
# Luego crear con el nuevo path
```

---

## 📊 VERIFICACIÓN FINAL

Ejecuta este script para verificar todo:

```powershell
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   VERIFICACIÓN POST-DEPLOYMENT" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════`n" -ForegroundColor Cyan

# 1. Puerto
Write-Host "[1/5] Verificando puerto 8080..." -ForegroundColor Yellow
if (Get-NetTCPConnection -LocalPort 8080 -ErrorAction SilentlyContinue) {
	Write-Host "✅ Puerto 8080 ESCUCHANDO`n" -ForegroundColor Green
} else {
	Write-Host "❌ Puerto 8080 NO está disponible`n" -ForegroundColor Red
}

# 2. API respondiendo
Write-Host "[2/5] Verificando respuesta de API..." -ForegroundColor Yellow
try {
	$resp = Invoke-WebRequest -Uri "http://192.168.10.10:8080/api/products/getproducts" -UseBasicParsing -TimeoutSec 3
	Write-Host "✅ API respondiendo (Status: $($resp.StatusCode))`n" -ForegroundColor Green
} catch {
	Write-Host "❌ API NO responde`n" -ForegroundColor Red
}

# 3. CORS headers
Write-Host "[3/5] Verificando headers CORS..." -ForegroundColor Yellow
try {
	$resp = Invoke-WebRequest -Uri "http://192.168.10.10:8080/api/products/getproducts" `
		-Headers @{"Origin"="http://192.168.10.10:9000"} `
		-UseBasicParsing -TimeoutSec 3
	$corsHeaders = $resp.Headers.Keys | Where-Object { $_ -like "Access-Control*" }
	if ($corsHeaders) {
		Write-Host "✅ Headers CORS presentes: $($corsHeaders.Count) headers" -ForegroundColor Green
		$corsHeaders | ForEach-Object { Write-Host "   - $_" }
	} else {
		Write-Host "❌ No hay headers CORS" -ForegroundColor Red
	}
	Write-Host ""
} catch {
	Write-Host "❌ Error: $($_.Exception.Message)`n" -ForegroundColor Red
}

# 4. BD conectada
Write-Host "[4/5] Verificando base de datos..." -ForegroundColor Yellow
try {
	$result = sqlcmd -S SERVER-ETIQUETAS -N -U Npino -P "Jossycar5%" -Q "SELECT DB_NAME();" -h-1
	if ($result -like "*RITRAMACLOUD*") {
		Write-Host "✅ Base de datos conectada`n" -ForegroundColor Green
	} else {
		Write-Host "⚠️  Respuesta inesperada de BD`n" -ForegroundColor Yellow
	}
} catch {
	Write-Host "❌ No se puede conectar a BD`n" -ForegroundColor Red
}

# 5. Blazor en 9000
Write-Host "[5/5] Verificando Blazor..." -ForegroundColor Yellow
try {
	$resp = Invoke-WebRequest -Uri "http://192.168.10.10:9000" -UseBasicParsing -TimeoutSec 3
	Write-Host "✅ Blazor está en línea (Status: $($resp.StatusCode))`n" -ForegroundColor Green
} catch {
	Write-Host "❌ Blazor no accesible`n" -ForegroundColor Yellow
}

Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   VERIFICACIÓN COMPLETADA" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
```

---

## 🎉 ¡LISTO!

Si todo está en verde (✅), tu problema de CORS se ha resuelto.

**Próximas acciones:**
1. ✅ Prueba desde el navegador
2. ✅ Realiza operaciones CRUD
3. ✅ Verifica que no haya errores en Console (F12)
4. ✅ Monitorea los logs

¿Necesitas más ayuda? 🚀
