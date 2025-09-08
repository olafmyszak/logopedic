using LogopedicBackend.Data;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;

namespace LogopedicBackend.IntegrationTests;

public class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
{
    protected TestDataSeeder DataSeeder { get; }
    protected HttpClient Client { get; private set; }

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LogopedicContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        DataSeeder = new TestDataSeeder(dbContext, userManager);
        Client = factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        return DataSeeder.SeedAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}