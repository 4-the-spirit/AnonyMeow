namespace AnonyMeow.Dtos.Posts;

// A post's tag is mandatory, so re-assigning it never accepts null (no "clear the tag" option).
public record UpdatePostFlairRequest(Guid FlairId);
