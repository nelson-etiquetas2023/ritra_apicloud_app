namespace Shared.Security
{
    public static class AppPolicies
    {
        public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";
        public const string AdminOnly = "AdminOnly";
        public const string UserOrAdmin = "UserOrAdmin";
    }
}
