using Microsoft.JSInterop;
using System.Text.Json;


namespace WEB.Services.LocalStorage
{
    public class LocalStorage(IJSRuntime JS) : ILocalStorage
    {
        public IJSRuntime JS { get; set; } = JS;

        private readonly Dictionary<string, string> _memory = new();
        private bool _storageAvailable = true;

        public bool StorageAvailable => _storageAvailable;

        public async Task SetItemAsync<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);

            bool saved;
            try
            {
                saved = await JS.InvokeAsync<bool>("setItem", key, json);
            }
            catch
            {
                saved = false;
            }

            if (saved)
            {
                _storageAvailable = true;
            }
            else
            {
                _storageAvailable = false;
                _memory[key] = json;
            }
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            string? json = null;
            try
            {
                json = await JS.InvokeAsync<string>("getItem", key);
            }
            catch
            {
                json = null;
            }

            if (string.IsNullOrEmpty(json))
                _memory.TryGetValue(key, out json);

            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task RemoveItemAsync(string key)
        {
            _memory.Remove(key);
            try
            {
                await JS.InvokeVoidAsync("deleteItem", key);
            }
            catch
            {
                // Sin acceso al almacenamiento, solo se limpia la memoria.
            }
        }

        public async Task ClearAsync()
        {
            _memory.Clear();
            try
            {
                await JS.InvokeVoidAsync("clearStorage");
            }
            catch
            {
                // Sin acceso al almacenamiento.
            }
        }
    }
}
