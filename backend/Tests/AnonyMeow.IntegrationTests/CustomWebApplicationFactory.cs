using AnonyMeow.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AnonyMeow.IntegrationTests;

public class CustomWebApplicationFactory(string connectionString, string environmentName = "Testing") : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString
            });
        });

        // Swaps real Azure AD B2C token validation for the test-only signing key so tests can
        // mint their own tokens via TestJwtTokenFactory without a live B2C tenant.
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestJwtTokenFactory.Issuer,
                    ValidateAudience = true,
                    ValidAudience = TestJwtTokenFactory.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TestJwtTokenFactory.SigningKey,
                    ValidateLifetime = true
                };
            });
        });
    }
}
