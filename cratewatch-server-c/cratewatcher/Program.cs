using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CrateWatch.Artists.Repositories;
using CrateWatch.Artists.Services;
using CrateWatch.Configuration;
using CrateWatch.CoverArt;
using CrateWatch.Db;
using CrateWatch.MusicBrainz;
using CrateWatch.Releases.Repositories;
using CrateWatch.TheAudioDb;
using CrateWatch.Users.Cli;
using CrateWatch.Users.Repositories;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Same environment variables (and defaults) as the Java backend's application.yml.
string Setting(string key, string fallback) => config[key] is { Length: > 0 } value ? value : fallback;

var connectionString = new NpgsqlConnectionStringBuilder
{
    Host = Setting("DB_HOST", "localhost"),
    Port = int.Parse(Setting("DB_PORT", "5432")),
    Database = Setting("DB_NAME", "cdwatcher"),
    Username = Setting("DB_USER", "cdwatcher"),
    Password = Setting("DB_PASSWORD", "cdwatcher"),
}.ConnectionString;

var jwtSecret = config["JWT_GENERATOR_SIGNATURE_SECRET"];
if (string.IsNullOrEmpty(jwtSecret))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("JWT_GENERATOR_SIGNATURE_SECRET must be set outside Development.");
    }
    jwtSecret = "pleaseChangeThisSecretForANewOne";
}
// HS256 needs a key of at least 256 bits.
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("JWT_GENERATOR_SIGNATURE_SECRET must be at least 32 bytes long.");
}
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddDbContext<CrateWatchDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// Traffic arrives via NPMplus and then the frontend's nginx, so trust two X-Forwarded-For hops to see the real client IP.
// shortcut: trusts the header from any peer; safe only because the backend port is not published outside Docker.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.ForwardLimit = 2;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// shortcut: per-IP only; add a per-username limit if someone ever sprays one account from many IPs.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();

builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = signingKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            NameClaimType = "sub",
        };
        // A valid token for a user that no longer exists is rejected here, so controllers can trust the "uid" claim.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var username = context.Principal?.Identity?.Name;
                var user = username is null
                    ? null
                    : await context.HttpContext.RequestServices.GetRequiredService<UserRepository>().FindByUsernameAsync(username);
                if (user is null)
                {
                    context.Fail("Unknown user");
                    return;
                }
                ((ClaimsIdentity)context.Principal!.Identity!).AddClaim(new Claim(TokenService.UserIdClaim, user.Id.ToString()));
            },
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton(new TokenService(signingKey));

builder.Services.AddSingleton(new StoragePaths(
    CoverArt: Setting("COVER_ART_STORAGE_PATH", "./cover-art-cache"),
    ArtistPhoto: Setting("ARTIST_PHOTO_STORAGE_PATH", "./artist-photo-cache")));

var readTimeout = TimeSpan.FromSeconds(10);

builder.Services.AddHttpClient<MusicBrainzClient>(client =>
{
    client.BaseAddress = new Uri(config["Services:MusicBrainz"]!);
    client.Timeout = readTimeout;
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CrateWatch/0.1 ( thijmen@verkerkdigital.nl )");
});
builder.Services.AddHttpClient<CoverArtArchiveClient>(client =>
{
    client.BaseAddress = new Uri(config["Services:CoverArtArchive"]!);
    client.Timeout = readTimeout;
});
builder.Services.AddHttpClient<TheAudioDbClient>(client =>
{
    client.BaseAddress = new Uri($"{config["Services:TheAudioDb"]!.TrimEnd('/')}/{Setting("THEAUDIODB_API_KEY", "123")}/");
    client.Timeout = readTimeout;
});
builder.Services.AddHttpClient(ArtistPhotoCacheService.ImageHttpClientName, client =>
    {
        client.Timeout = readTimeout;
        client.MaxResponseContentBufferSize = 5_000_000;
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Services.AddHttpClient(CrateWatch.Shops.Services.ShopScanner.HttpClientName, client =>
    {
        client.Timeout = readTimeout;
        client.MaxResponseContentBufferSize = 2_000_000;
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (compatible; CrateWatch/0.1)");
    })
    .ConfigurePrimaryHttpMessageHandler(CrateWatch.Shops.Services.ShopHttpHandler.Create);

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ArtistRepository>();
builder.Services.AddScoped<UserArtistRepository>();
builder.Services.AddScoped<ReleaseRepository>();
builder.Services.AddScoped<UserReleaseStateRepository>();
builder.Services.AddScoped<ArtistImportService>();
builder.Services.AddScoped<ArtistService>();
builder.Services.AddScoped<ArtistQueryService>();
builder.Services.AddScoped<ArtistPhotoService>();
builder.Services.AddScoped<CrateWatch.Releases.Services.ReleaseService>();
builder.Services.AddScoped<CrateWatch.Users.Services.AuthService>();
builder.Services.AddScoped<CrateWatch.Shops.Repositories.ShopRepository>();
builder.Services.AddScoped<CrateWatch.Shops.Services.ShopScanner>();
builder.Services.AddScoped<CrateWatch.Shops.Services.ShopService>();
builder.Services.AddSingleton<CrateWatch.Caching.ImageFileCache>();
builder.Services.AddScoped<CoverArtCacheService>();
builder.Services.AddScoped<ArtistPhotoCacheService>();

var app = builder.Build();

DatabaseMigrator.Migrate(connectionString, app.Logger);

if (args is ["create-user", var newUsername])
{
    Environment.ExitCode = await CreateUserCommand.RunAsync(app.Services, newUsername);
    return;
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

await app.RunAsync();
