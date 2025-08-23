using LogopedicBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        using var context = scope.ServiceProvider.GetRequiredService<LogopedicContext>();
        
        context.Database.Migrate();
    }
}