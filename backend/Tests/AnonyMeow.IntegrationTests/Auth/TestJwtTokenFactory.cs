using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace AnonyMeow.IntegrationTests.Auth;

// Test-only JWT issuer: mints self-signed tokens with an arbitrary `oid` claim so integration
// tests can exercise authenticated/authorized endpoints without a real Azure AD B2C tenant.
// CustomWebApplicationFactory swaps the app's JwtBearer validation parameters to trust tokens
// signed with this same key — never used outside the integration test project.
public static class TestJwtTokenFactory
{
    public const string Issuer = "https://anonymeow.tests/";
    public const string Audience = "anonymeow-tests";

    private static readonly SymmetricSecurityKey SigningKeyMaterial =
        new(System.Text.Encoding.UTF8.GetBytes("test-only-signing-key-not-for-production-use-0123456789"));

    public static SecurityKey SigningKey => SigningKeyMaterial;

    public static string CreateToken(string oid)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(SigningKeyMaterial, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("oid", oid)],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return handler.WriteToken(token);
    }
}
