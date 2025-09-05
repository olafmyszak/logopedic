using LogopedicBackend.Constants;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;

namespace LogopedicBackend.Extensions;

public static class SeedAdminExtension
{
    public static async Task SeedAdminAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        const string adminEmail = "admin@logopedic.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            logger.LogWarning($"Admin user with email {adminEmail} not found");
            return;
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            logger.LogInformation($"Admin user {adminEmail} assigned to role {AppRoles.Admin}");
        }
    }
}