namespace AnonyMeow.Common.Exceptions;

// The CIAM user flow is demanding attributes this app doesn't collect during native-auth sign-up
// (e.g. "Display Name" left enabled on the user flow even though CompleteProfilePage already
// collects it in-app). This is a portal misconfiguration, not a user error — 500 so it's logged
// and surfaced loudly rather than silently degrading the sign-up experience.
public class NativeAuthAttributesRequiredException(IReadOnlyList<string> attributeNames)
    : ApiException(
        StatusCodes.Status500InternalServerError,
        "Sign-Up Configuration Error",
        $"The CIAM user flow requires attributes this app doesn't collect ({string.Join(", ", attributeNames)}). " +
        "Remove them from the user flow's required attributes in the Entra admin center.");
