using LogopedicBackend.Constants;
using Microsoft.AspNetCore.Identity;

namespace LogopedicBackend.Extensions;

public static class SeedRolesExtension
{
    public static async Task SeedRolesAsync(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = [AppRoles.Therapist, AppRoles.Admin];

        foreach (string role in roles)
        {
            bool roleExists = await roleManager.RoleExistsAsync(role);
            if (!roleExists)
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
