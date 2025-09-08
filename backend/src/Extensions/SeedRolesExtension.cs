using LogopedicBackend.Constants;
using Microsoft.AspNetCore.Identity;

namespace LogopedicBackend.Extensions;

public static class SeedRolesExtension
{
    public static async Task SeedRolesAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = [AppRoles.Therapist, AppRoles.Admin];

        foreach (var role in roles)
        {
            var roleExists = await roleManager.RoleExistsAsync(role);
            if (!roleExists)
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}