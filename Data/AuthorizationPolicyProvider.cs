using Microsoft.AspNetCore.Authorization;


namespace webMVC.Data
{
    public static class AuthorizationPolicyProvider
    {
        public static void AddCustomPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("ViewManageMenu", builder =>
            {
                builder.RequireAuthenticatedUser();
                builder.RequireRole(RoleName.Administrator);
            });
        }
    }
}
