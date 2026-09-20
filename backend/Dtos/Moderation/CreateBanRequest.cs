namespace AnonyMeow.Dtos.Moderation;

// The plan's DTO listing named only Reason; Username was an evident omission — a ban target has
// to be identified somehow, and the route only carries the community name.
public record CreateBanRequest(string Username, string Reason);
