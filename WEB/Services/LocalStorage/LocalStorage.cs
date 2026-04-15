using Microsoft.JSInterop;
using System.Text.Json;


namespace WEB.Services.LocalStorage
{
    public class LocalStorage(IJSRuntime JS) : ILocalStorage
    {
        public IJSRuntime JS { get; set; } = JS;

        public async Task SetItemAsync<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            await JS.InvokeVoidAsync("setItem", key, json);
        }
        public async Task<T?> GetItemAsync<T>(string key)
        {
            var json = await JS.InvokeAsync<string>("getItem", key);

            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }
        public async Task RemoveItemAsync(string key)
        {
            await JS.InvokeVoidAsync("localStorage.removeItem", key);
        }
        public async Task ClearAsync()
        {
            await JS.InvokeVoidAsync("localStorage.clear"); 
        }

       

       

    
    }
}
