using LogopedicBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using LogopedicContext context = scope.ServiceProvider.GetRequiredService<LogopedicContext>();

        context.Database.Migrate();
    }
}
