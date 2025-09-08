using System.Data.Common;
using LogopedicBackend.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LogopedicBackend.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services
                .SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<LogopedicContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbConnectionDescriptor = services
                .SingleOrDefault(s => s.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor is not null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            var connectionString = _dbContainer.GetConnectionString() + ";Include Error Detail=true";
            services.AddDbContext<LogopedicContext>(options => { options.UseNpgsql(connectionString); });
        });

        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Force the test host to use the Identity cookie scheme as the default
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultSignInScheme = "Test"; // optional, safe to set
                    options.DefaultScheme = "Test"; // optional convenience
                })
                // Register our test handler under the same name Identity uses
                .AddScheme<AuthenticationSchemeOptions, TestCookieAuthHandler>("Test", _ => { });
        });
    }

    public Task InitializeAsync()
    {
        return _dbContainer.StartAsync();
    }

    public new Task DisposeAsync()
    {
        return _dbContainer.StopAsync();
    }
}