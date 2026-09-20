using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AnonyMeow.Common.Development;

// Development-only JWT issuer: mints self-signed tokens with an `oid` claim so Postman/local
// testing can exercise authenticated endpoints without a real Azure AD B2C tenant. Program.cs
// only trusts tokens signed with this key when IsDevelopment() is true — never used in
// Production/Staging, where the real B2C Authority/Audience are enforced instead.
public static class DevJwtTokenFactory
{
    public const string Issuer = "https://anonymeow.local-dev/";
    public const string Audience = "anonymeow-local-dev";

    private static readonly SymmetricSecurityKey SigningKeyMaterial =
        new(Encoding.UTF8.GetBytes("dev-only-signing-key-not-for-production-use-0123456789"));

    public static SecurityKey SigningKey => SigningKeyMaterial;

    public static string CreateToken(string oid)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(SigningKeyMaterial, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("oid", oid)],
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return handler.WriteToken(token);
    }
}
