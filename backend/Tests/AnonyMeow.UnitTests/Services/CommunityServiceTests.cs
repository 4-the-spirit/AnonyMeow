using AnonyMeow.Common;
using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class CommunityServiceTests
{
    private const string DefaultIcon = "https://example.com/icon.png";
    private const string DefaultBanner = "https://example.com/banner.png";

    // Accepts every image by default — CommunityService itself is what's under test here, not
    // image validation (see PostServiceTests/ImageUploadService for that).
    private class FakeImageUploadService(bool throwOwnershipMismatch = false) : IImageUploadService
    {
        public Task<(string UploadUrl, string BlobUrl)> CreateUploadSasAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(("https://upload.example/sas", "https://blob.example/image.png"));

        public Task ValidateImageAsync(string blobUrl, Guid ownerId, CancellationToken cancellationToken = default) =>
            throwOwnershipMismatch ? throw new ImageOwnershipMismatchException(blobUrl) : Task.CompletedTask;
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CommunityService CreateService(AppDbContext dbContext, IImageUploadService? imageUploadService = null) =>
        new(dbContext, imageUploadService ?? new FakeImageUploadService());

    private static async Task<AppUser> SeedUserAsync(AppDbContext dbContext)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            B2CObjectId = Guid.NewGuid().ToString(),
            Username = $"user{Guid.NewGuid():N}"[..10],
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateAsync_AddsCreatorAsModerator()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);

        var community = await service.CreateAsync("testcommunity", "desc", null, creator.Id, null, DefaultIcon, DefaultBanner);

        var membership = await dbContext.CommunityMemberships.SingleAsync(
            m => m.CommunityId == community.Id && m.AppUserId == creator.Id);
        Assert.Equal(CommunityRole.Moderator, membership.Role);
    }

    [Fact]
    public async Task CreateAsync_PersistsIconAndBannerImageUrls()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);

        var community = await service.CreateAsync(
            "imagedcommunity", "desc", null, creator.Id, null,
            iconImageUrl: "https://example.com/custom-icon.png", bannerImageUrl: "https://example.com/custom-banner.png");

        Assert.Equal("https://example.com/custom-icon.png", community.IconImageUrl);
        Assert.Equal("https://example.com/custom-banner.png", community.BannerImageUrl);
    }

    [Fact]
    public async Task UpdateAsync_SetsIconAndBannerImageUrls_WhenProvided()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);
        var community = await service.CreateAsync("updateimagecommunity", null, null, creator.Id, null, DefaultIcon, DefaultBanner);

        await service.UpdateAsync(
            community, creator.Id, description: null, rules: null,
            iconImageUrl: "https://example.com/new-icon.png", bannerImageUrl: "https://example.com/new-banner.png");

        Assert.Equal("https://example.com/new-icon.png", community.IconImageUrl);
        Assert.Equal("https://example.com/new-banner.png", community.BannerImageUrl);
    }

    [Fact]
    public async Task UpdateAsync_LeavesExistingImageUrls_WhenNotProvided()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);
        var community = await service.CreateAsync(
            "keepimagecommunity", null, null, creator.Id, null,
            iconImageUrl: "https://example.com/icon.png", bannerImageUrl: "https://example.com/banner.png");

        await service.UpdateAsync(community, creator.Id, description: "updated desc", rules: null);

        Assert.Equal("https://example.com/icon.png", community.IconImageUrl);
        Assert.Equal("https://example.com/banner.png", community.BannerImageUrl);
        Assert.Equal("updated desc", community.Description);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenIconImageOwnedByAnotherUser()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var community = await CreateService(dbContext).CreateAsync(
            "badownercommunity", null, null, creator.Id, null, DefaultIcon, DefaultBanner);

        // A separate service instance (same dbContext) with a throwing image validator, so only
        // the UpdateAsync call under test is exercised against it — CreateAsync above must
        // succeed first to have a community to update.
        var serviceWithRejectingValidator = CreateService(dbContext, new FakeImageUploadService(throwOwnershipMismatch: true));
        await Assert.ThrowsAsync<ImageOwnershipMismatchException>(() => serviceWithRejectingValidator.UpdateAsync(
            community, creator.Id, description: null, rules: null, iconImageUrl: "https://example.com/other-user-icon.png"));
    }

    [Fact]
    public async Task CreateAsync_DoesNotValidateImages_EvenWithRejectingValidator()
    {
        // Icon/banner are mandatory (non-blank) but deliberately not run through
        // ImageUploadService.ValidateImageAsync at creation — see the comment in
        // CommunityService.CreateAsync for why (Blob Storage isn't provisioned yet). Unlike
        // UpdateAsync, a rejecting validator should have no effect here.
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext, new FakeImageUploadService(throwOwnershipMismatch: true));

        var community = await service.CreateAsync(
            "badownercreate", null, null, creator.Id, null, "https://example.com/other-user-icon.png", DefaultBanner);

        Assert.Equal("https://example.com/other-user-icon.png", community.IconImageUrl);
    }

    [Fact]
    public async Task CreateAsync_Throws_OnDuplicateName()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);
        await service.CreateAsync("dupcommunity", null, null, creator.Id, null, DefaultIcon, DefaultBanner);

        await Assert.ThrowsAsync<CommunityNameConflictException>(
            () => service.CreateAsync("dupcommunity", null, null, creator.Id, null, DefaultIcon, DefaultBanner));
    }

    [Fact]
    public async Task JoinAsync_IsIdempotent()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var member = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);
        var community = await service.CreateAsync("joincommunity", null, null, creator.Id, null, DefaultIcon, DefaultBanner);

        await service.JoinAsync(community, member.Id);
        await service.JoinAsync(community, member.Id);

        var count = await dbContext.CommunityMemberships.CountAsync(
            m => m.CommunityId == community.Id && m.AppUserId == member.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task LeaveAsync_BlocksSoleModeratorFromLeaving()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);
        var community = await service.CreateAsync("solemodcommunity", null, null, creator.Id, null, DefaultIcon, DefaultBanner);

        await Assert.ThrowsAsync<SoleModeratorLeaveException>(() => service.LeaveAsync(community, creator.Id));
    }

    [Fact]
    public async Task LeaveAsync_AllowsModeratorToLeave_WhenAnotherModeratorRemains()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var otherMod = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);
        var community = await service.CreateAsync("multimodcommunity", null, null, creator.Id, null, DefaultIcon, DefaultBanner);
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = community.Id,
            AppUserId = otherMod.Id,
            Role = CommunityRole.Moderator,
            JoinedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        await service.LeaveAsync(community, creator.Id);

        var stillMember = await dbContext.CommunityMemberships.AnyAsync(
            m => m.CommunityId == community.Id && m.AppUserId == creator.Id);
        Assert.False(stillMember);
    }

    [Fact]
    public async Task CreateAsync_SeedsDefaultFlairs()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);

        var community = await service.CreateAsync("defaultflaircommunity", null, null, creator.Id, null, DefaultIcon, DefaultBanner);

        var flairs = await dbContext.Flairs.Where(f => f.CommunityId == community.Id).ToListAsync();
        Assert.Equal(DefaultFlairs.Names.Count, flairs.Count);
        Assert.All(flairs, f => Assert.True(f.IsDefault));
        Assert.All(flairs, f => Assert.Equal(DefaultFlairs.NameToColorHex[f.Name], f.ColorHex));
        Assert.Equal(DefaultFlairs.Names.OrderBy(n => n), flairs.Select(f => f.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task CreateAsync_SeedsDefaultFlairs_AlongsideOptionalCustomFlairs()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);

        var community = await service.CreateAsync(
            "customflaircommunity", null, null, creator.Id,
            flairs: [new CreateFlairRequest("Custom", "#123456")],
            iconImageUrl: DefaultIcon, bannerImageUrl: DefaultBanner);

        var flairs = await dbContext.Flairs.Where(f => f.CommunityId == community.Id).ToListAsync();
        Assert.Equal(DefaultFlairs.Names.Count + 1, flairs.Count);
        Assert.Contains(flairs, f => f.Name == "Custom" && !f.IsDefault);
    }

    [Fact]
    public async Task CreateAsync_PersistsRules_InSubmittedOrder()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);

        var community = await service.CreateAsync(
            "rulescommunity", null,
            [new CommunityRuleRequest("Be nice", "No insults"), new CommunityRuleRequest("No spam", "Don't repeat posts")],
            creator.Id, null, DefaultIcon, DefaultBanner);

        var rules = await service.ListRulesAsync(community.Id);
        Assert.Equal(2, rules.Count);
        Assert.Equal("Be nice", rules[0].Title);
        Assert.Equal("No spam", rules[1].Title);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesEntireRuleSet_WhenRulesProvided()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);
        var community = await service.CreateAsync(
            "replacerulescommunity", null, [new CommunityRuleRequest("Old rule", "Old description")],
            creator.Id, null, DefaultIcon, DefaultBanner);

        await service.UpdateAsync(
            community, creator.Id, description: null,
            rules: [new CommunityRuleRequest("New rule", "New description")]);

        var rules = await service.ListRulesAsync(community.Id);
        Assert.Single(rules);
        Assert.Equal("New rule", rules[0].Title);
    }

    [Fact]
    public async Task UpdateAsync_LeavesExistingRules_WhenRulesNotProvided()
    {
        var dbContext = CreateDbContext();
        var creator = await SeedUserAsync(dbContext);
        var service = CreateService(dbContext);
        var community = await service.CreateAsync(
            "keeprulescommunity", null, [new CommunityRuleRequest("Kept rule", "Kept description")],
            creator.Id, null, DefaultIcon, DefaultBanner);

        await service.UpdateAsync(community, creator.Id, description: "updated desc", rules: null);

        var rules = await service.ListRulesAsync(community.Id);
        Assert.Single(rules);
        Assert.Equal("Kept rule", rules[0].Title);
    }
}
