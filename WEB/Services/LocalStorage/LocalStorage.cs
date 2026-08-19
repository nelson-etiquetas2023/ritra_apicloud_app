using Microsoft.JSInterop;
using System.Text.Json;

namespace WEB.Services.LocalStorage
{
    public enum StorageMode
    {
        Local,
        Session
    }

    public class LocalStorage : ILocalStorage
    {
        private readonly IJSRuntime JS;

        private readonly Dictionary<string, string> _memory = new();
        private bool _storageAvailable = true;

        private readonly StorageMode _mode;

        public bool StorageAvailable => _storageAvailable;

        public LocalStorage(IJSRuntime JS, StorageMode mode = StorageMode.Local)
        {
            this.JS = JS;
            _mode = mode;
        }

        private string GetStorageFunctionPrefix()
        {
            // Siempre usamos setItem/getItem/deleteItem (definidos en index.html apuntando a sessionStorage)
            return "Item";
        }

        private async Task<bool> SetItemJsonAsync(string key, string json)
        {
            string funcName = $"set{GetStorageFunctionPrefix()}Item";
            try
            {
                var result = await JS.InvokeAsync<bool>(funcName, key, json);
                return result;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string?> GetItemJsonAsync(string key)
        {
            string funcName = $"get{GetStorageFunctionPrefix()}Item";
            try
            {
                return await JS.InvokeAsync<string>(funcName, key);
            }
            catch
            {
                return null;
            }
        }

        private async Task DeleteItemJsonAsync(string key)
        {
            string funcName = $"delete{GetStorageFunctionPrefix()}Item";
            try
            {
                await JS.InvokeVoidAsync(funcName, key);
            }
            catch { }
        }

        private async Task ClearStorageJsonAsync()
        {
            string funcName = $"clear{GetStorageFunctionPrefix()}Storage";
            try
            {
                await JS.InvokeVoidAsync(funcName);
            }
            catch { }
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);

            bool saved;
            try
            {
                saved = await SetItemJsonAsync(key, json);
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
                json = await GetItemJsonAsync(key);
            }
            catch { }

            if (string.IsNullOrEmpty(json))
                _memory.TryGetValue(key, out json);

            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task RemoveItemAsync(string key)
        {
            try
            {
                await DeleteItemJsonAsync(key);
                _memory.Remove(key);
            }
            catch { }
        }

        public async Task ClearAsync()
        {
            try
            {
                await ClearStorageJsonAsync();
            }
            catch { }
            _memory.Clear();
        }
    }
}