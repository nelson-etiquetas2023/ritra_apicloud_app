#!/usr/bin/env pwsh
<#
.SYNOPSIS
Script de diagnóstico para CORS entre API y Blazor

.DESCRIPTION
Verifica puertos, conectividad y configuración CORS

.EXAMPLE
.\diagnostico-cors.ps1
#>

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     DIAGNÓSTICO DE CORS - API vs BLAZOR                  ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Variables
$API_HOST = "192.168.10.10"
$API_PORT = 8080
$BLAZOR_PORT = 9000
$API_URL = "http://$API_HOST`:$API_PORT"
$BLAZOR_URL = "http://$API_HOST`:$BLAZOR_PORT"

# ============================================
# 1. Verificar puertos en escucha
# ============================================
Write-Host "[1/5] Verificando puertos en escucha..." -ForegroundColor Yellow

$netstat = netstat -ano | findstr ":$API_PORT"
if ($netstat) {
	Write-Host "✅ Puerto $API_PORT está en escucha" -ForegroundColor Green
	Write-Host "   $netstat`n" -ForegroundColor DarkGray
} else {
	Write-Host "❌ Puerto $API_PORT NO está en escucha" -ForegroundColor Red
	Write-Host "   ℹ️  La API podría no estar corriendo`n" -ForegroundColor Yellow
}

# ============================================
# 2. Prueba de conectividad a la API
# ============================================
Write-Host "[2/5] Probando conectividad a la API..." -ForegroundColor Yellow

try {
	$response = Invoke-WebRequest -Uri "$API_URL/api/health" -ErrorAction Stop -UseBasicParsing
	Write-Host "✅ API está respondiendo" -ForegroundColor Green
	Write-Host "   Status: $($response.StatusCode)`n" -ForegroundColor DarkGray
} catch {
	Write-Host "⚠️  No se pudo conectar a $API_URL" -ForegroundColor Yellow
	Write-Host "   Error: $($_.Exception.Message)`n" -ForegroundColor DarkGray
}

# ============================================
# 3. Prueba CORS préflight (OPTIONS)
# ============================================
Write-Host "[3/5] Probando solicitud CORS preflight (OPTIONS)..." -ForegroundColor Yellow

try {
	$corsHeaders = @{
		"Origin" = "http://192.168.10.10:9000"
		"Access-Control-Request-Method" = "POST"
		"Access-Control-Request-Headers" = "content-type,authorization"
	}

	$response = Invoke-WebRequest -Uri "$API_URL/api/auth/login" `
		-Method OPTIONS `
		-Headers $corsHeaders `
		-ErrorAction Stop `
		-UseBasicParsing

	Write-Host "✅ Preflight OPTIONS exitoso" -ForegroundColor Green
	Write-Host "   Status: $($response.StatusCode)" -ForegroundColor DarkGray
	Write-Host "   Headers de respuesta:" -ForegroundColor DarkGray

	$response.Headers.Keys | Where-Object { $_ -like "Access-Control*" } | ForEach-Object {
		Write-Host "   - $_: $($response.Headers[$_])" -ForegroundColor Cyan
	}
	Write-Host ""

} catch {
	Write-Host "⚠️  Error en preflight OPTIONS" -ForegroundColor Yellow
	Write-Host "   Error: $($_.Exception.Message)`n" -ForegroundColor DarkGray
}

# ============================================
# 4. Prueba solicitud GET simple
# ============================================
Write-Host "[4/5] Probando solicitud GET a /api/products/getproducts..." -ForegroundColor Yellow

try {
	$headers = @{
		"Origin" = "http://192.168.10.10:9000"
	}

	$response = Invoke-WebRequest -Uri "$API_URL/api/products/getproducts" `
		-Headers $headers `
		-ErrorAction Stop `
		-UseBasicParsing

	Write-Host "✅ Solicitud GET exitosa" -ForegroundColor Green
	Write-Host "   Status: $($response.StatusCode)" -ForegroundColor DarkGray
	Write-Host "   Headers CORS en respuesta:" -ForegroundColor DarkGray

	$corsHeadersFound = $false
	$response.Headers.Keys | Where-Object { $_ -like "Access-Control*" } | ForEach-Object {
		$corsHeadersFound = $true
		Write-Host "   - $_: $($response.Headers[$_])" -ForegroundColor Cyan
	}

	if (-not $corsHeadersFound) {
		Write-Host "   ⚠️  No hay headers CORS en la respuesta" -ForegroundColor Yellow
	}
	Write-Host ""

} catch {
	Write-Host "❌ Error en solicitud GET" -ForegroundColor Red
	Write-Host "   Error: $($_.Exception.Message)`n" -ForegroundColor Yellow
}

# ============================================
# 5. Información del sistema
# ============================================
Write-Host "[5/5] Información del sistema..." -ForegroundColor Yellow
Write-Host "   PowerShell: $($PSVersionTable.PSVersion)" -ForegroundColor DarkGray
Write-Host "   OS: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)" -ForegroundColor DarkGray
Write-Host "   Host: $API_HOST" -ForegroundColor DarkGray
Write-Host "   API URL: $API_URL" -ForegroundColor DarkGray
Write-Host "   Blazor URL: $BLAZOR_URL`n" -ForegroundColor DarkGray

# ============================================
# Resumen
# ============================================
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    RECOMENDACIONES                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Host "1️⃣  Si la API NO está respondiendo:" -ForegroundColor Yellow
Write-Host "   - Verifica que está corriendo en http://$API_HOST`:$API_PORT" -ForegroundColor DarkGray
Write-Host "   - Compila y ejecuta: dotnet run`n" -ForegroundColor DarkGray

Write-Host "2️⃣  Si ve error CORS en el navegador:" -ForegroundColor Yellow
Write-Host "   - Abre: $BLAZOR_URL en el navegador" -ForegroundColor DarkGray
Write-Host "   - Presiona F12 para Developer Tools" -ForegroundColor DarkGray
Write-Host "   - Ve a Network y realiza una acción" -ForegroundColor DarkGray
Write-Host "   - Verifica los Response Headers`n" -ForegroundColor DarkGray

Write-Host "3️⃣  Headers CORS esperados:" -ForegroundColor Yellow
Write-Host "   - Access-Control-Allow-Origin: http://192.168.10.10:9000" -ForegroundColor Cyan
Write-Host '   - Access-Control-Allow-Methods: GET, POST, PUT, DELETE' -ForegroundColor Cyan
Write-Host "   - Access-Control-Allow-Headers: ..." -ForegroundColor Cyan
Write-Host "   - Access-Control-Allow-Credentials: true`n" -ForegroundColor Cyan

Write-Host "4️⃣  Si el puerto está en uso por otra aplicación:" -ForegroundColor Yellow
Write-Host "   - Ejecuta: Get-Process -Id (Get-NetTCPConnection -LocalPort $API_PORT).OwningProcess" -ForegroundColor DarkGray
Write-Host "   - Termina el proceso o usa otro puerto`n" -ForegroundColor DarkGray
