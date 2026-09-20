using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Conversations;
using AnonyMeow.Dtos.Messages;
using AnonyMeow.Dtos.Moderation;
using AnonyMeow.Dtos.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ConversationAndMessageEndpointsTests(PostgresContainerFixture postgresFixture)
{
    private CustomWebApplicationFactory CreateFactory() => new(postgresFixture.ConnectionString);

    private static async Task<(HttpClient Client, string Username)> CreateClientWithCompletedProfileAsync(
        CustomWebApplicationFactory factory, string usernamePrefix)
    {
        var oid = Guid.NewGuid().ToString();
        var username = $"{usernamePrefix}{Guid.NewGuid():N}"[..15];
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(oid));

        var response = await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, username, "seed"));
        response.EnsureSuccessStatusCode();

        return (client, username);
    }

    private static async Task<Guid> GetUserIdAsync(CustomWebApplicationFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalized = username.ToLowerInvariant();
        return await dbContext.Users
            .Where(u => u.Username != null && u.Username.ToLower() == normalized)
            .Select(u => u.Id)
            .SingleAsync();
    }

    [Fact]
    public async Task StartConversation_ListAndSendMessages_HappyPath()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "dma");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmb");
        var aId = await GetUserIdAsync(factory, aUsername);
        var bId = await GetUserIdAsync(factory, bUsername);

        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);
        Assert.Equal(bUsername, conversation!.OtherUsername);
        Assert.Equal(bId, conversation.OtherUserId);

        var listAResponse = await a.GetAsync("/api/conversations");
        var listA = await listAResponse.Content.ReadFromJsonAsync<PagedResponse<ConversationResponse>>(TestJsonOptions.Default);
        Assert.Contains(listA!.Items, c => c.Id == conversation.Id && c.OtherUsername == bUsername && c.OtherUserId == bId);

        var listBResponse = await b.GetAsync("/api/conversations");
        var listB = await listBResponse.Content.ReadFromJsonAsync<PagedResponse<ConversationResponse>>(TestJsonOptions.Default);
        Assert.Contains(listB!.Items, c => c.Id == conversation.Id && c.OtherUsername == aUsername && c.OtherUserId == aId);

        var sendResponse = await a.PostAsJsonAsync($"/api/conversations/{conversation.Id}/messages", new SendMessageRequest("hello!"));
        Assert.Equal(HttpStatusCode.Created, sendResponse.StatusCode);

        var messagesResponse = await b.GetAsync($"/api/conversations/{conversation.Id}/messages");
        Assert.Equal(HttpStatusCode.OK, messagesResponse.StatusCode);
        var messages = await messagesResponse.Content.ReadFromJsonAsync<PagedResponse<MessageResponse>>(TestJsonOptions.Default);
        Assert.Single(messages!.Items);
        Assert.Equal("hello!", messages.Items[0].Body);

        // Re-requesting the same pair returns the same conversation (idempotent get-or-create).
        var restartResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var restarted = await restartResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);
        Assert.Equal(conversation.Id, restarted!.Id);
    }

    [Fact]
    public async Task StartConversation_WithSelf_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmself");

        var response = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(aUsername));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ToBlockedUser_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmblocka");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmblockb");

        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        // b blocks a after the conversation already exists — must still be enforced at send-time.
        var meResponse = await a.GetAsync("/api/users/me");
        var me = await meResponse.Content.ReadFromJsonAsync<UserResponse>(TestJsonOptions.Default);
        await b.PostAsync($"/api/users/{me!.Username}/block", null);

        var sendResponse = await a.PostAsJsonAsync($"/api/conversations/{conversation!.Id}/messages", new SendMessageRequest("still there?"));

        Assert.Equal(HttpStatusCode.Forbidden, sendResponse.StatusCode);
    }

    [Fact]
    public async Task StartConversation_WithBlockedUser_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmblkstarta");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmblkstartb");

        await b.PostAsync($"/api/users/{aUsername}/block", null);

        var response = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AccessConversation_AsNonParticipant_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmnpa");
        var (_, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmnpb");
        var (intruder, _) = await CreateClientWithCompletedProfileAsync(factory, "dmnpc");

        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        var listMessagesResponse = await intruder.GetAsync($"/api/conversations/{conversation!.Id}/messages");
        Assert.Equal(HttpStatusCode.NotFound, listMessagesResponse.StatusCode);

        var sendResponse = await intruder.PostAsJsonAsync($"/api/conversations/{conversation.Id}/messages", new SendMessageRequest("hi"));
        Assert.Equal(HttpStatusCode.NotFound, sendResponse.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ToNonExistentConversation_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmghost");

        var response = await a.PostAsJsonAsync($"/api/conversations/{Guid.NewGuid()}/messages", new SendMessageRequest("hi"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReportMessage_ByParticipant_Succeeds_ByNonParticipant_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmrepa");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmrepb");
        var (intruder, _) = await CreateClientWithCompletedProfileAsync(factory, "dmrepc");

        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);
        var sendResponse = await a.PostAsJsonAsync($"/api/conversations/{conversation!.Id}/messages", new SendMessageRequest("reportable"));
        var message = await sendResponse.Content.ReadFromJsonAsync<MessageResponse>(TestJsonOptions.Default);

        var reportResponse = await b.PostAsJsonAsync($"/api/messages/{message!.Id}/reports", new CreateReportRequest(ReportReasonCategory.Harassment, "harassment"));
        Assert.Equal(HttpStatusCode.Created, reportResponse.StatusCode);

        var intruderReportResponse = await intruder.PostAsJsonAsync($"/api/messages/{message.Id}/reports", new CreateReportRequest(ReportReasonCategory.Harassment, "harassment"));
        Assert.Equal(HttpStatusCode.NotFound, intruderReportResponse.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ContainingEmail_IsBlockedWithDetectedCategories()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmpiia");
        var (_, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmpiib");
        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        var response = await a.PostAsJsonAsync(
            $"/api/conversations/{conversation!.Id}/messages", new SendMessageRequest("email me at someone@example.com"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("detectedCategories", body);
        Assert.Contains("Email", body);
    }

    [Fact]
    public async Task GetConversation_ByParticipant_Succeeds_ByNonParticipant_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmgeta");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmgetb");
        var (intruder, _) = await CreateClientWithCompletedProfileAsync(factory, "dmgetc");

        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        var getResponse = await a.GetAsync($"/api/conversations/{conversation!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);
        Assert.Equal(bUsername, fetched!.OtherUsername);

        var intruderResponse = await intruder.GetAsync($"/api/conversations/{conversation.Id}");
        Assert.Equal(HttpStatusCode.NotFound, intruderResponse.StatusCode);

        var ghostResponse = await a.GetAsync($"/api/conversations/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, ghostResponse.StatusCode);
    }

    [Fact]
    public async Task SendMessage_AsReply_IncludesReplyPreview_AndRejectsForeignReplyTarget()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmreplya");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmreplyb");
        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        var originalResponse = await a.PostAsJsonAsync(
            $"/api/conversations/{conversation!.Id}/messages", new SendMessageRequest("what time works for you?"));
        var original = await originalResponse.Content.ReadFromJsonAsync<MessageResponse>(TestJsonOptions.Default);

        var replyResponse = await b.PostAsJsonAsync(
            $"/api/conversations/{conversation.Id}/messages", new SendMessageRequest("3pm works", original!.Id));
        Assert.Equal(HttpStatusCode.Created, replyResponse.StatusCode);
        var reply = await replyResponse.Content.ReadFromJsonAsync<MessageResponse>(TestJsonOptions.Default);
        Assert.Equal(original.Id, reply!.ReplyToMessageId);
        Assert.Equal("what time works for you?", reply.ReplyToBodyPreview);

        // A message id from a completely different conversation must be rejected.
        var (_, otherBUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmreplyd");
        var otherStartResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(otherBUsername));
        var otherConversation = await otherStartResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);
        var foreignReplyResponse = await a.PostAsJsonAsync(
            $"/api/conversations/{otherConversation!.Id}/messages", new SendMessageRequest("nice try", original.Id));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, foreignReplyResponse.StatusCode);
    }

    [Fact]
    public async Task GetConversation_ExposesOtherParticipantsLastSeenAt()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmseena");
        var (_, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmseenb");

        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        var getResponse = await a.GetAsync($"/api/conversations/{conversation!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        // b has completed its profile (an authenticated request), so CurrentUserAccessor has
        // already stamped a LastSeenAt for it.
        Assert.NotNull(fetched!.OtherUserLastSeenAt);
        Assert.True(DateTimeOffset.UtcNow - fetched.OtherUserLastSeenAt!.Value < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PinConversation_ReflectsOnlyForTheCallingParticipant()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmpina");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmpinb");
        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        var pinResponse = await a.PatchAsJsonAsync($"/api/conversations/{conversation!.Id}/pin", new SetConversationPinRequest(true));
        Assert.Equal(HttpStatusCode.OK, pinResponse.StatusCode);
        var pinned = await pinResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);
        Assert.True(pinned!.IsPinned);

        var bGetResponse = await b.GetAsync($"/api/conversations/{conversation.Id}");
        var bView = await bGetResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);
        Assert.False(bView!.IsPinned);

        var unpinResponse = await a.PatchAsJsonAsync($"/api/conversations/{conversation.Id}/pin", new SetConversationPinRequest(false));
        var unpinned = await unpinResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);
        Assert.False(unpinned!.IsPinned);
    }

    [Fact]
    public async Task DeleteConversation_HidesFromListForCallerOnly_UntilNewMessageRevivesIt()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmdela");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmdelb");
        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        var deleteResponse = await a.DeleteAsync($"/api/conversations/{conversation!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listAResponse = await a.GetAsync("/api/conversations");
        var listA = await listAResponse.Content.ReadFromJsonAsync<PagedResponse<ConversationResponse>>(TestJsonOptions.Default);
        Assert.DoesNotContain(listA!.Items, c => c.Id == conversation.Id);

        var listBResponse = await b.GetAsync("/api/conversations");
        var listB = await listBResponse.Content.ReadFromJsonAsync<PagedResponse<ConversationResponse>>(TestJsonOptions.Default);
        Assert.Contains(listB!.Items, c => c.Id == conversation.Id);

        await b.PostAsJsonAsync($"/api/conversations/{conversation.Id}/messages", new SendMessageRequest("you still there?"));

        var revivedListAResponse = await a.GetAsync("/api/conversations");
        var revivedListA = await revivedListAResponse.Content.ReadFromJsonAsync<PagedResponse<ConversationResponse>>(TestJsonOptions.Default);
        Assert.Contains(revivedListA!.Items, c => c.Id == conversation.Id);
    }

    [Fact]
    public async Task PinAndDeleteConversation_AsNonParticipant_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dmpnpa");
        var (_, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dmpnpb");
        var (intruder, _) = await CreateClientWithCompletedProfileAsync(factory, "dmpnpc");
        var startResponse = await a.PostAsJsonAsync("/api/conversations", new StartConversationRequest(bUsername));
        var conversation = await startResponse.Content.ReadFromJsonAsync<ConversationResponse>(TestJsonOptions.Default);

        var pinResponse = await intruder.PatchAsJsonAsync($"/api/conversations/{conversation!.Id}/pin", new SetConversationPinRequest(true));
        Assert.Equal(HttpStatusCode.NotFound, pinResponse.StatusCode);

        var deleteResponse = await intruder.DeleteAsync($"/api/conversations/{conversation.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}
