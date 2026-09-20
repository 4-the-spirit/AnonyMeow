namespace AnonyMeow.Dtos.NativeAuth;

// Oid is extracted server-side from the id_token (see NativeAuthEndpoints) so the frontend never
// needs to decode a JWT itself — it just stores these fields the same way it already stores the
// dev-bypass token/oid pair.
public record AuthTokensResponse(string AccessToken, string IdToken, string? RefreshToken, string Oid, int ExpiresIn);
