using System.Security.Claims;
using System.Text.Encodings.Web;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LogopedicBackend.IntegrationTests;

public class TestCookieAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        UserManager<User> userManager = Context.RequestServices.GetRequiredService<UserManager<User>>();
        // Has to match one of the users created in TestDataSeeder
        User? user = await userManager.FindByNameAsync(TestDataSeeder.TestUsername);

        if (user == null)
        {
            return AuthenticateResult.Fail("Test user not found");
        }

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        IList<string> roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
