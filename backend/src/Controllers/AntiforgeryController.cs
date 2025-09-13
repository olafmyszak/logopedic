using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Controllers;

public record TokenResponse(string Token);

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AntiforgeryController(IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<TokenResponse> GetToken()
    {
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(HttpContext);

        if (tokens.RequestToken is null)
        {
            return Problem(title: "Antiforgery token unavailable",
                detail: "No request token could be generated. Check antiforgery configuration.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(new TokenResponse(tokens.RequestToken));
    }
}
