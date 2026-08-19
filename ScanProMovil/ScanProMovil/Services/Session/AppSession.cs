using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.Maui.Devices;

namespace ScanProMovil.Services.Session
{
    public class AppSession
    {
        private const string WarehouseKey = "warehouse_name";
        private const string DeviceNameKey = "device_name";
        private const string DeviceCodeFallbackKey = "device_code_fallback";

        private bool _deviceInitialized;

        public string? UserId { get; private set; }
        public string? UserName { get; private set; }
        public string? UserEmail { get; private set; }
        public string? UserRole { get; private set; }

        public string WarehouseName
        {
            get => Preferences.Get(WarehouseKey, "ALMACEN-01");
            set => Preferences.Set(WarehouseKey, value);
        }

        public string DeviceName
        {
            get => Preferences.Get(DeviceNameKey, defaultDeviceName);
            set => Preferences.Set(DeviceNameKey, value);
        }

        public string DeviceCode { get; private set; } = string.Empty;

        public bool IsAuthenticated => !string.IsNullOrEmpty(UserEmail);

        public string UserDisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(UserName) && string.IsNullOrWhiteSpace(UserEmail))
                    return string.Empty;
                if (string.IsNullOrWhiteSpace(UserName)) return UserEmail ?? string.Empty;
                if (string.IsNullOrWhiteSpace(UserEmail)) return UserName ?? string.Empty;
                return $"{UserName} - {UserEmail}";
            }
        }

        public string DeviceDisplayName
        {
            get
            {
                var model = DeviceName;
                var mac = DeviceCode;
                if (string.IsNullOrWhiteSpace(model)) return mac;
                if (string.IsNullOrWhiteSpace(mac)) return model;
                return $"{model} - {mac}";
            }
        }

        private static readonly string defaultDeviceName = GetDeviceFriendlyName();

        public void InitializeDevice()
        {
            if (_deviceInitialized) return;
            _deviceInitialized = true;

            DeviceCode = ReadDeviceMac();

            if (string.IsNullOrWhiteSpace(DeviceCode))
            {
                var fallback = Preferences.Get(DeviceCodeFallbackKey, string.Empty);
                if (string.IsNullOrWhiteSpace(fallback))
                {
                    fallback = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
                    Preferences.Set(DeviceCodeFallbackKey, fallback);
                }
                DeviceCode = fallback;
            }

            if (string.IsNullOrWhiteSpace(DeviceName))
            {
                var name = GetDeviceFriendlyName();
                if (!string.IsNullOrWhiteSpace(name))
                    DeviceName = name;
            }
        }

        public void SetUserFromToken(string jwt)
        {
            var claims = ParseJwtClaims(jwt);
            if (claims is null)
            {
                Logout();
                return;
            }

            UserId = claims.Value.id;
            UserName = claims.Value.name;
            UserEmail = claims.Value.email;
            UserRole = claims.Value.role;
        }

        public void Logout()
        {
            UserId = null;
            UserName = null;
            UserEmail = null;
            UserRole = null;
        }

        private static string ReadDeviceMac()
        {
            try
            {
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    var address = networkInterface.GetPhysicalAddress().ToString();
                    if (string.IsNullOrWhiteSpace(address) || address.Length < 12)
                        continue;

                    var name = networkInterface.Name.ToLowerInvariant();
                    if (name.StartsWith("wlan") || name.StartsWith("wifi") ||
                        name.StartsWith("eth") || name.StartsWith("radio") ||
                        name.StartsWith("rmnet") || name.StartsWith("usb") ||
                        name.StartsWith("wlan0"))
                    {
                        return FormatMac(address);
                    }
                }
            }
            catch
            {
                // se ignora y se usa el fallback
            }

            return string.Empty;
        }

        private static string FormatMac(string mac)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < mac.Length; i += 2)
            {
                if (sb.Length > 0) sb.Append(':');
                sb.Append(mac, i, 2);
            }
            return sb.ToString();
        }

        private static string GetDeviceFriendlyName()
        {
            try
            {
                return DeviceInfo.Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static (string? id, string? name, string? email, string? role)? ParseJwtClaims(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return null;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var bytes = Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(bytes);

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                string Get(params string[] names)
                {
                    foreach (var name in names)
                    {
                        if (root.TryGetProperty(name, out var value))
                            return value.ToString();
                    }
                    return string.Empty;
                }

                return (
                    Get("nameid", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"),
                    Get("unique_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"),
                    Get("email", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"),
                    Get("role", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                );
            }
            catch
            {
                return null;
            }
        }
    }
}