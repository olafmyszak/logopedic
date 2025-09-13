using LogopedicBackend.Data;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;
using Xunit.Abstractions;

namespace LogopedicBackend.IntegrationTests;

public class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
{
    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory, ITestOutputHelper testOutputHelper)
    {
        IServiceScope scope = factory.Services.CreateScope();
        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        DbContext = scope.ServiceProvider.GetRequiredService<LogopedicContext>();
        DataSeeder = new TestDataSeeder(DbContext, userManager);
        Client = factory.CreateClient();
        TestOutputHelper = testOutputHelper;
    }

    protected TestDataSeeder DataSeeder { get; }
    protected HttpClient Client { get; private set; }
    protected LogopedicContext DbContext { get; }
    protected ITestOutputHelper TestOutputHelper { get; }

    public Task InitializeAsync() => DataSeeder.SeedAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
