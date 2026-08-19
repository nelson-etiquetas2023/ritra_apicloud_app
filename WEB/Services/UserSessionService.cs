using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WEB.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IJSRuntime _js;

        public UserSessionService(IJSRuntime js)
        {
            _js = js;
        }

        public string UserName { get; set; } = "";

        public string DeviceCode { get; set; } = "";

        public string DeviceName { get; set; } = "";

        public async Task SetUserSession(string userName, string deviceCode, string deviceName)
        {
            UserName = userName;
            DeviceCode = deviceCode;
            DeviceName = deviceName;

            await _js.InvokeVoidAsync("setSessionStorageItem", "userName", userName);
            await _js.InvokeVoidAsync("setSessionStorageItem", "deviceCode", deviceCode);
            await _js.InvokeVoidAsync("setSessionStorageItem", "deviceName", deviceName);
        }

        public async Task LoadUserSession()
        {
            UserName = await _js.InvokeAsync<string>("getSessionStorageItem", "userName") ?? "";
            DeviceCode = await _js.InvokeAsync<string>("getSessionStorageItem", "deviceCode") ?? "";
            DeviceName = await _js.InvokeAsync<string>("getSessionStorageItem", "deviceName") ?? "";
        }

        public async Task ClearUserSession()
        {
            UserName = "";
            DeviceCode = "";
            DeviceName = "";
            await _js.InvokeVoidAsync("clearSessionStorage");
        }
    }
}