using Microsoft.AspNetCore.Components;

namespace WEB.Services
{
    public interface IUserSessionService
    {
        string UserName { get; set; }

        string DeviceCode { get; set; }

        string DeviceName { get; set; }

        Task SetUserSession(string userName, string deviceCode, string deviceName);

        Task LoadUserSession();

        Task ClearUserSession();
    }
}