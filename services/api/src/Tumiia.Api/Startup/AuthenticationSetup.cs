using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Tumiia.Infrastructure.Identity;

namespace Tumiia.Api.Startup;

/// <summary>Checks the JWT access token on each request.</summary>
public static class AuthenticationSetup
{
    /// <summary>
    /// JWT bearer authentication using the same settings that sign the tokens. Claims keep their short JWT names
    /// (<c>sub</c>, <c>role</c>). Which endpoints need a signed-in user is set in <see cref="ControllersSetup"/>.
    /// </summary>
    public static IServiceCollection AddTumiiaAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = jwt.Value.CreateValidationParameters();
            });

        services.AddAuthorization();

        return services;
    }
}
