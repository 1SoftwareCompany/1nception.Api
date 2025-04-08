using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace One.Inception.Api.Security;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.Fail("Ignore this message.(To be fixed soon...)"));
    }
}

public sealed class BasicAuthorizationAttribute : AuthorizeAttribute
{
    public const string AuthenticationSchema = "BasicAuthentication";
    public const string PolicyName = "Basic";

    public BasicAuthorizationAttribute()
    {
        Policy = PolicyName;
        AuthenticationSchemes = AuthenticationSchema;
    }
}
