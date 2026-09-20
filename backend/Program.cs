using System.IO.Compression;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AnonyMeow.Common.Authorization;
using AnonyMeow.Common.Development;
using AnonyMeow.Common.Middleware;
using AnonyMeow.Common.Options;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Endpoints;
using AnonyMeow.Hubs;
using AnonyMeow.Services;
using AnonyMeow.Services.CommentValidation;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.PiiDetection;
using AnonyMeow.Services.NativeAuth;
using AnonyMeow.Services.PostValidation;
using AnonyMeow.Services.Ranking;
using AnonyMeow.Services.Ranking.Strategies;
using AnonyMeow.Services.SpamDetection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// No endpoint accepts raw file uploads (images go direct-to-Blob-Storage via SAS, see
// ImageUploadService), so every request body is JSON text — 1 MB is generous headroom while still
// rejecting oversized payloads (e.g. a huge PollOptions/ImageUrls array) before model binding.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_000_000);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Minimal API endpoints bind request bodies via System.Text.Json, which defaults to
// serializing enums as integers. Clients send enums as strings (e.g. SortOrder's "Hot"),
// so without this converter every request body containing an enum fails to deserialize with
// a 500 "Failed to read parameter ... as JSON" error.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Every response here is JSON over HTTPS (list endpoints in particular can return sizeable
// payloads), so compressing them shrinks transfer time on slower connections. EnableForHttps is
// safe here since responses carry no reflected per-request secrets for a BREACH-style attack to
// exploit — auth is a bearer token in a header, not a cookie/CSRF token echoed into the body.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IUsernameReservationService, UsernameReservationService>();
builder.Services.AddScoped<IAuthorizationHandler, ProfileCompletionHandler>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<IAuthorizationHandler, CommunityModeratorHandler>();

builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
builder.Services.AddScoped<IPostRequestValidator, PostRequestValidator>();

builder.Services.AddScoped<IVotingService, VotingService>();
builder.Services.AddScoped<IMentionParsingService, MentionParsingService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ICommentRequestValidator, CommentRequestValidator>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddKeyedScoped<ISortStrategy, NewSortStrategy>(SortOrder.New);
builder.Services.AddKeyedScoped<ISortStrategy, TopSortStrategy>(SortOrder.Top);
builder.Services.AddKeyedScoped<ISortStrategy, HotSortStrategy>(SortOrder.Hot);
builder.Services.AddKeyedScoped<ISortStrategy, ControversialSortStrategy>(SortOrder.Controversial);
builder.Services.AddKeyedScoped<ISortStrategy, TrendingSortStrategy>(SortOrder.Trending);
builder.Services.AddKeyedScoped<ISortStrategy, PinnedSortStrategy>(SortOrder.Pinned);

builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IModerationActionService, ModerationActionService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<IFlairService, FlairService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddScoped<ISavedPostService, SavedPostService>();
builder.Services.AddScoped<ISavedCommentService, SavedCommentService>();
builder.Services.AddScoped<IFeedService, FeedService>();

builder.Services.AddScoped<IPlatformAdminService, PlatformAdminService>();
builder.Services.AddScoped<IAuthorizationHandler, PlatformAdminHandler>();

builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddScoped<ICommunityRecommendationService, CommunityRecommendationService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Composite pattern — every registered detector runs.
builder.Services.AddScoped<IPiiDetector, EmailPatternDetector>();
builder.Services.AddScoped<IPiiDetector, PhoneNumberDetector>();
builder.Services.AddScoped<IPiiDetector, AddressHeuristicDetector>();
builder.Services.AddScoped<IPiiDetectionService, CompositePiiDetectionService>();
builder.Services.AddScoped<IContentSubmissionPipeline, ContentSubmissionPipeline>();

// Same Strategy + Composite shape as PII detection above — every registered heuristic runs.
builder.Services.AddScoped<ISpamHeuristic, PostingFrequencyHeuristic>();
builder.Services.AddScoped<ISpamHeuristic, DuplicateContentHeuristic>();
builder.Services.AddScoped<ISpamHeuristic, LinkSpamHeuristic>();
builder.Services.AddScoped<ISpamDetectionService, CompositeSpamDetectionService>();
builder.Services.AddScoped<ISpamFlaggingService, SpamFlaggingService>();

builder.Services.Configure<AzureAdB2COptions>(
    builder.Configuration.GetSection(AzureAdB2COptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName));
builder.Services.Configure<ReactionOptions>(
    builder.Configuration.GetSection(ReactionOptions.SectionName));
builder.Services.Configure<PostImageOptions>(
    builder.Configuration.GetSection(PostImageOptions.SectionName));
builder.Services.Configure<PiiDetectionOptions>(
    builder.Configuration.GetSection(PiiDetectionOptions.SectionName));
builder.Services.Configure<SpamDetectionOptions>(
    builder.Configuration.GetSection(SpamDetectionOptions.SectionName));

// Tenant values are sourced from the "AzureAdB2C" config section (empty placeholders in
// appsettings.json; real values via user-secrets/App Configuration — never committed, and
// never provisioned without explicit confirmation since it's a shared Azure resource). Until
// real values are supplied, Authority/Audience are null and any endpoint that actually
// requires a validated token will fail metadata discovery at request time.
//
// This targets Microsoft Entra External ID (CIAM), not classic Azure AD B2C — B2C stopped
// accepting new tenants May 1, 2025. CIAM tenants get their own dedicated *.ciamlogin.com
// subdomain, so unlike B2C's {instance}/{domain}/{policy}/v2.0 shape, the authority is just
// the bare instance with no domain or policy segment and no /v2.0 suffix, e.g.
// https://anonymeow.ciamlogin.com/ (confirmed against Microsoft's CIAM quickstart docs —
// appending /{domain}/v2.0 here, a carryover from the B2C URL shape, made metadata discovery
// fail and broke sign-in).
var b2cOptions = builder.Configuration
    .GetSection(AzureAdB2COptions.SectionName)
    .Get<AzureAdB2COptions>();

// Native authentication (custom email/password sign-up/sign-in UI, see NativeAuthEndpoints)
// reuses this same CIAM app registration — no separate config section, secrets, or Bicep params.
// Its base URL shape (both segments use the tenant's subdomain, confirmed against Microsoft's
// native-auth API reference) is https://{subdomain}.ciamlogin.com/{subdomain}.onmicrosoft.com,
// derived here from AzureAdB2COptions.Instance's host.
builder.Services.Configure<NativeAuthOptions>(options =>
{
    if (string.IsNullOrEmpty(b2cOptions?.Instance) || string.IsNullOrEmpty(b2cOptions.ClientId))
    {
        // Mirrors the Authority/Audience null-until-configured pattern below — native auth calls
        // fail with a clear error at request time until real values are supplied.
        options.BaseUrl = string.Empty;
        options.ClientId = string.Empty;
        return;
    }

    var subdomain = new Uri(b2cOptions.Instance).Host.Split('.')[0];
    options.BaseUrl = $"https://{subdomain}.ciamlogin.com/{subdomain}.onmicrosoft.com/";
    options.ClientId = b2cOptions.ClientId;
});

builder.Services.AddHttpClient<INativeAuthClient, NativeAuthClient>((serviceProvider, client) =>
{
    var baseUrl = serviceProvider.GetRequiredService<IOptions<NativeAuthOptions>>().Value.BaseUrl;
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }

    // Default HttpClient.Timeout is 100s — without an explicit bound here, a hung call to
    // Microsoft's native-auth endpoint could leave a sign-up/sign-in/reset request hanging for
    // that long before the user sees any error.
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddScoped<ISignUpFlowService, SignUpFlowService>();
builder.Services.AddScoped<ISignInFlowService, SignInFlowService>();
builder.Services.AddScoped<IPasswordResetFlowService, PasswordResetFlowService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // No real B2C tenant is configured locally, so Authority-based metadata discovery
            // would fail every request. Trust self-signed tokens from DevJwtTokenFactory instead
            // (see /api/dev/token in DevAuthEndpoints, also dev-only) so Postman/local testing
            // can exercise authenticated endpoints. Never reached in Production/Staging.
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = DevJwtTokenFactory.Issuer,
                ValidateAudience = true,
                ValidAudience = DevJwtTokenFactory.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = DevJwtTokenFactory.SigningKey,
                ValidateLifetime = true
            };
        }
        else
        {
            // The bare Instance host has no `.well-known/openid-configuration` of its own (it
            // 404s) — CIAM only serves OIDC metadata under the tenant-scoped v2.0 path, the same
            // shape NativeAuthOptions.BaseUrl derives above.
            options.Authority = string.IsNullOrEmpty(b2cOptions?.Instance)
                ? null
                : $"{b2cOptions.Instance.TrimEnd('/')}/{new Uri(b2cOptions.Instance).Host.Split('.')[0]}.onmicrosoft.com/v2.0";
            options.Audience = string.IsNullOrEmpty(b2cOptions?.ClientId) ? null : b2cOptions.ClientId;
        }

        // Without this, JwtSecurityTokenHandler's default inbound claim map silently renames
        // "oid" to a long schemas.microsoft.com URI, breaking every FindFirst("oid") lookup
        // (ICurrentUserAccessor, request logging enrichment) even though the token is valid.
        options.MapInboundClaims = false;

        // Browser WebSocket/SSE transports used by the SignalR JS client can't set an
        // Authorization header on the handshake request, so the standard workaround is to send
        // the bearer token via an "access_token" query string param instead, promoted here to
        // the real token for any request under /hubs. Applies to both dev and B2C tokens.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new ProfileCompletionRequirement())
        .Build())
    .AddPolicy("AuthenticatedOnly", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("CommunityModerator", policy => policy.AddRequirements(new CommunityModeratorRequirement()))
    .AddPolicy("PlatformAdmin", policy => policy.AddRequirements(new PlatformAdminRequirement()));

// Global baseline limiter: partitioned per authenticated user id, falling back to remote IP for
// anonymous callers.
var rateLimitingOptions = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

// Phase 8: named per-action policies layered on top of the baseline above — applied via
// .RequireRateLimiting("...") on the CreatePost/CreateComment/SendMessage/Vote/Report endpoints.
var actionRateLimitOptions = builder.Configuration
    .GetSection(ActionRateLimitOptions.SectionName)
    .Get<ActionRateLimitOptions>() ?? new ActionRateLimitOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(RateLimitPartitionKeys.Resolve(httpContext), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitingOptions.PermitLimit,
            Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
            QueueLimit = rateLimitingOptions.QueueLimit
        }));

    void AddActionPolicy(string name, RateLimitPolicySettings settings) =>
        options.AddPolicy(name, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(RateLimitPartitionKeys.Resolve(httpContext), _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0
            }));

    AddActionPolicy("CreatePost", actionRateLimitOptions.CreatePost);
    AddActionPolicy("CreateComment", actionRateLimitOptions.CreateComment);
    AddActionPolicy("SendMessage", actionRateLimitOptions.SendMessage);
    AddActionPolicy("Vote", actionRateLimitOptions.Vote);
    AddActionPolicy("Report", actionRateLimitOptions.Report);
    AddActionPolicy("ImageUpload", actionRateLimitOptions.ImageUpload);
    AddActionPolicy("SignUpStart", actionRateLimitOptions.SignUpStart);
    AddActionPolicy("SignUpVerify", actionRateLimitOptions.SignUpVerify);
    AddActionPolicy("SignInStart", actionRateLimitOptions.SignInStart);
    AddActionPolicy("PasswordReset", actionRateLimitOptions.PasswordReset);
    AddActionPolicy("RefreshToken", actionRateLimitOptions.RefreshToken);

    // A10/A09: rejections are otherwise silent — logging them gives visibility into abuse/brute
    // force patterns without changing the 429 response itself.
    options.OnRejected = (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");
        logger.LogWarning(
            "Rate limit exceeded for {PartitionKey} on {Method} {Path}",
            RateLimitPartitionKeys.Resolve(context.HttpContext),
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);
        return ValueTask.CompletedTask;
    };
});

// SignalR's JSON hub protocol has its own serializer options, entirely separate from
// ConfigureHttpJsonOptions above — without this, enums pushed over the hub (e.g.
// NotificationResponse.Type) serialize as raw integers while the identical object returned from
// a REST endpoint serializes as a string, an inconsistency real clients would trip over.
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Frontend dev origin is configuration-driven (never hardcoded/branched by environment), per
// CLAUDE.md's "configuration differences go through configuration, not code branches." No
// AllowCredentials() — auth is a Bearer header, not cookies.
const string FrontendCorsPolicy = "FrontendDev";
var frontendOrigin = builder.Configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapDevAuthEndpoints();
}

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    // HSTS shouldn't be sent over local http, so it's gated to non-Development, per the standard
    // ASP.NET Core template pattern.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseResponseCompression();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();

// Runs after authentication (so the oid claim is populated) but before authorization, so
// requests are logged even when authorization short-circuits them with a 401/403.
app.UseMiddleware<RequestLoggingEnrichmentMiddleware>();

app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthEndpoints();
app.MapNativeAuthEndpoints();
app.MapUserEndpoints();
app.MapCommunityEndpoints();
app.MapPostEndpoints();
app.MapCommentEndpoints();
app.MapModerationEndpoints();
app.MapFlairEndpoints();
app.MapFriendshipEndpoints();
app.MapNotificationEndpoints();
app.MapBlockEndpoints();
app.MapConversationEndpoints();
app.MapMessageEndpoints();
app.MapSavedPostEndpoints();
app.MapSavedCommentEndpoints();
app.MapFeedEndpoints();
app.MapAdminEndpoints();
app.MapDiscoverEndpoints();
app.MapSearchEndpoints();

app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

public partial class Program;