using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityRule> CommunityRules => Set<CommunityRule>();
    public DbSet<CommunityMembership> CommunityMemberships => Set<CommunityMembership>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostImage> PostImages => Set<PostImage>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<PollVote> PollVotes => Set<PollVote>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<CommunityBan> CommunityBans => Set<CommunityBan>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();
    public DbSet<Flair> Flairs => Set<Flair>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<PiiDetectionLog> PiiDetectionLogs => Set<PiiDetectionLog>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
    public DbSet<SavedComment> SavedComments => Set<SavedComment>();
    public DbSet<SpamFlag> SpamFlags => Set<SpamFlag>();
    public DbSet<PlatformRestriction> PlatformRestrictions => Set<PlatformRestriction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Phase 10 full-text search: NpgsqlTsVector is an Npgsql-only CLR type the InMemory
        // provider (used by unit tests) can't map, so this is gated on the real provider rather
        // than living in the per-entity IEntityTypeConfiguration classes. Ignored (not just
        // unconfigured) under InMemory so model validation doesn't try to map it at all.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<Post>(builder =>
            {
                builder.HasGeneratedTsVectorColumn(p => p.SearchVector!, "english", p => new { p.Title, p.BodyMarkdown });
                builder.HasIndex(p => p.SearchVector).HasMethod("GIN");
            });

            modelBuilder.Entity<Comment>(builder =>
            {
                builder.HasGeneratedTsVectorColumn(c => c.SearchVector!, "english", c => new { c.BodyMarkdown });
                builder.HasIndex(c => c.SearchVector).HasMethod("GIN");
            });

            modelBuilder.Entity<Community>(builder =>
            {
                builder.HasGeneratedTsVectorColumn(c => c.SearchVector!, "english", c => new { c.Name, c.Description });
                builder.HasIndex(c => c.SearchVector).HasMethod("GIN");
            });
        }
        else
        {
            modelBuilder.Entity<Post>().Ignore(p => p.SearchVector);
            modelBuilder.Entity<Comment>().Ignore(c => c.SearchVector);
            modelBuilder.Entity<Community>().Ignore(c => c.SearchVector);
        }
    }
}
