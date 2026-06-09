using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public class TrainingAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TrainingAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If the request has no X-Training-User header then unauthenticated 
        if (!Request.Headers.ContainsKey("X-Training-User"))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Missing training user header."));
        }

        // If it has the header allowed through
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, Request.Headers["X-Training-User"]!)
        };

        var identity  = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}