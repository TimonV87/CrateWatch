using System.Security.Claims;
using System.Text;
using CrateWatch.Artists.Repositories;
using CrateWatch.Artists.Services;
using CrateWatch.Configuration;
using CrateWatch.CoverArt;
using CrateWatch.Db;
using CrateWatch.MusicBrainz;
using CrateWatch.Releases.Repositories;
using CrateWatch.Releases.Services;
using CrateWatch.TheAudioDb;
using CrateWatch.Users.Repositories;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CrateWatch/0.1 ( https://github.com/tv/cratewatch )");
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
builder.Services.AddHttpClient(ArtistPhotoCacheService.ImageHttpClientName);

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ArtistRepository>();
builder.Services.AddScoped<UserArtistRepository>();
builder.Services.AddScoped<ReleaseRepository>();
builder.Services.AddScoped<UserReleaseStateRepository>();
builder.Services.AddScoped<ArtistImportService>();
builder.Services.AddScoped<ArtistService>();
builder.Services.AddSingleton<CrateWatch.Caching.ImageFileCache>();
builder.Services.AddScoped<CoverArtCacheService>();
builder.Services.AddScoped<ArtistPhotoCacheService>();

var app = builder.Build();

DatabaseMigrator.Migrate(connectionString, app.Logger);

app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
