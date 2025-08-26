using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;

namespace LogopedicBackend.Extensions;

public static class SeedAdminExtension
{
    public static async Task SeedAdminAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        const string role = "Admin";

        var adminExists = await roleManager.RoleExistsAsync("Admin");
        if (!adminExists)
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            logger.LogInformation($"Created {role} role");
        }

        const string adminEmail = "admin@logopedic.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            logger.LogWarning($"Admin user with email {adminEmail} not found");
            return;
        }

        if (!await userManager.IsInRoleAsync(adminUser, role))
        {
            await userManager.AddToRoleAsync(adminUser, role);
            logger.LogInformation($"Admin user {adminEmail} assigned to role {role}");
        }
    }
}