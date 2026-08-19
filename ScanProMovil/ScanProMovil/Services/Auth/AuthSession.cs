namespace ScanProMovil.Services.Auth
{
    public class AuthSession
    {
        private const string TokenKey = "auth_token";
        private const string AttemptsKey = "login_attempts";
        private const string BlockedUntilKey = "blocked_until";

        public const int MaxAttempts = 5;

        public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(15);

        public int FailedAttempts
        {
            get => Preferences.Get(AttemptsKey, 0);
            set => Preferences.Set(AttemptsKey, value);
        }

        public DateTimeOffset? BlockedUntil
        {
            get
            {
                var seconds = Preferences.Get(BlockedUntilKey, 0L);
                return seconds == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(seconds);
            }
            set
            {
                if (value.HasValue)
                    Preferences.Set(BlockedUntilKey, value.Value.ToUnixTimeSeconds());
                else
                    Preferences.Remove(BlockedUntilKey);
            }
        }

        public bool IsBlocked => BlockedUntil is { } until && until > DateTimeOffset.Now;

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(TokenKey);
            }
            catch
            {
                return null;
            }
        }

        public async Task SetTokenAsync(string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                SecureStorage.Remove(TokenKey);
            }
            else
            {
                await SecureStorage.SetAsync(TokenKey, token);
            }
        }

        public void Reset()
        {
            FailedAttempts = 0;
            BlockedUntil = null;
        }
    }
}