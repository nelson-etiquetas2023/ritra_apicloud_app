# 🛠️ CONFIGURACIONES ALTERNATIVAS SEGÚN TIPO DE DEPLOYMENT

## 📌 Escenario 1: API corriendo directamente en puerto 8080 (kestrel)

**launchSettings.json:**
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

**Program.cs (Principal):**
```csharp
// Ya está configurado correctamente en tu versión actual
builder.Services.AddCors(options =>
{
	options.AddPolicy("PoliticaCORS", builder =>
	{
		builder
			.WithOrigins("http://192.168.10.10:9000")
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()
			.WithExposedHeaders("Content-Type", "Authorization", "X-Total-Count");
	});
});
```

---

## 🌐 Escenario 2: API detrás de IIS (reverse proxy en puerto 8080)

**Agregar en Program.cs ANTES de AddCors:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// ⚠️ IMPORTANTE: Agregar esto si está detrás de IIS
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
							   ForwardedHeaders.XForwardedProto |
							   ForwardedHeaders.XForwardedHost;
	options.KnownNetworks.Clear();
	options.KnownProxies.Clear();
});

// Luego el CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("PoliticaCORS", builder =>
	{
		builder
			.WithOrigins("http://192.168.10.10:9000")
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()
			.WithExposedHeaders("Content-Type", "Authorization", "X-Total-Count");
	});
});
```

**En la app:**
```csharp
var app = builder.Build();

// PRIMERO: ForwardedHeaders
app.UseForwardedHeaders();

// LUEGO: El resto del middleware
app.UseRouting();
app.UseCors("PoliticaCORS");
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
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## 🔗 Escenario 3: API detrás de NGINX (reverse proxy)

**Configuración NGINX:**
```nginx
upstream dotnet_api {
	server 127.0.0.1:5220;  # Puerto interno de Kestrel
}

server {
	listen 192.168.10.10:8080;
	server_name 192.168.10.10;

	# Aumentar tamaño de buffer para headers CORS
	proxy_buffer_size 128k;
	proxy_buffers 4 256k;
	proxy_busy_buffers_size 256k;

	location / {
		proxy_pass http://dotnet_api;
		proxy_set_header Host $host;
		proxy_set_header X-Real-IP $remote_addr;
		proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
		proxy_set_header X-Forwarded-Proto $scheme;
		proxy_set_header X-Forwarded-Host $host;
		proxy_set_header X-Forwarded-Port $server_port;

		# Permitir métodos CORS
		if ($request_method = 'OPTIONS') {
			add_header 'Access-Control-Allow-Origin' 'http://192.168.10.10:9000' always;
			add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS' always;
			add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Authorization' always;
			add_header 'Access-Control-Allow-Credentials' 'true' always;
			add_header 'Content-Length' 0 always;
			return 204;
		}
	}
}
```

**Program.cs para NGINX:**
```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
							   ForwardedHeaders.XForwardedProto |
							   ForwardedHeaders.XForwardedHost;
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("PoliticaCORS", builder =>
	{
		builder
			.WithOrigins("http://192.168.10.10:9000")
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()
			.WithExposedHeaders("Content-Type", "Authorization", "X-Total-Count");
	});
});
```

---

## 🔒 Escenario 4: HTTPS en producción (192.168.10.10:8443 o 8080 con SSL)

**launchSettings.json:**
```json
{
  "profiles": {
	"https": {
	  "commandName": "Project",
	  "dotnetRunMessages": true,
	  "launchBrowser": false,
	  "applicationUrl": "https://192.168.10.10:8080",
	  "environmentVariables": {
		"ASPNETCORE_ENVIRONMENT": "Production"
	  }
	}
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddCors(options =>
{
	options.AddPolicy("PoliticaCORS", builder =>
	{
		builder
			.WithOrigins("https://192.168.10.10:9000")  // ⚠️ HTTPS en lugar de HTTP
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()
			.WithExposedHeaders("Content-Type", "Authorization", "X-Total-Count");
	});
});
```

**En producción, usa certificado SSL:**
```powershell
# Generar certificado autofirmado (si no tienes uno)
$cert = New-SelfSignedCertificate -CertStoreLocation cert:\CurrentUser\My `
	-DnsName "192.168.10.10" `
	-FriendlyName "RitramaAPI" `
	-NotAfter (Get-Date).AddYears(10)

# Exportar y colocar en el servidor
```

---

## 🎯 ¿Cuál es tu setup actual?

Necesito saber para darte la configuración exacta:

**Pregunta 1: ¿Dónde corre la API?**
- [ ] Directamente: `dotnet run` en un servidor
- [ ] IIS (Windows Server)
- [ ] Docker/Linux
- [ ] Nginx reverse proxy
- [ ] Azure App Service
- [ ] Otro

**Pregunta 2: ¿Qué puerto usa Kestrel internamente?**
- [ ] 5220 (default en dev)
- [ ] 5000/5001
- [ ] Otro: ___________

**Pregunta 3: ¿Es HTTPS o HTTP?**
- [ ] HTTP solamente
- [ ] HTTPS con certificado
- [ ] HTTP en LAN, HTTPS en internet

---

## 🐛 Comandos útiles para diagnosticar

```powershell
# Ver qué está escuchando en puerto 8080
netstat -ano | findstr :8080

# Ver el proceso que usa ese puerto
Get-Process -Id (Get-NetTCPConnection -LocalPort 8080).OwningProcess

# Terminar el proceso por puerto
Stop-Process -Id (Get-NetTCPConnection -LocalPort 8080).OwningProcess -Force

# Probar conectividad
Test-NetConnection -ComputerName 192.168.10.10 -Port 8080

# Hacer request OPTIONS (CORS preflight)
curl -X OPTIONS http://192.168.10.10:8080/api/products/getproducts `
  -H "Origin: http://192.168.10.10:9000" `
  -H "Access-Control-Request-Method: GET" `
  -v
```

---

Proporciona respuestas a las "3 Preguntas" arriba y te daré la configuración exacta.
