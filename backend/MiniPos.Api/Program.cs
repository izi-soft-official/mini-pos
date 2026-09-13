using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Threading;
using MiniPos.Api.Data;

LoadEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(BuildPostgresConnectionString(builder.Configuration)));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "mini-pos API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? Array.Empty<string>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // In Development show Swagger UI. Seeding/migrations are intentionally not performed automatically.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void LoadEnvFile()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ".env")))
    {
        directory = directory.Parent;
    }

    if (directory is null)
    {
        return;
    }

    foreach (var line in File.ReadAllLines(Path.Combine(directory.FullName, ".env")))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            continue;
        }

        var separator = trimmed.IndexOf('=');
        if (separator <= 0)
        {
            continue;
        }

        var key = trimmed[..separator].Trim();
        if (Environment.GetEnvironmentVariable(key) is null)
        {
            Environment.SetEnvironmentVariable(key, trimmed[(separator + 1)..].Trim());
        }
    }
}

static string BuildPostgresConnectionString(IConfiguration configuration)
{
    // Prefer configuration, then environment, then sensible defaults for development.
    string Get(string key, string defaultValue)
        => configuration[key] ?? Environment.GetEnvironmentVariable(key) ?? defaultValue;

    var host = Get("POSTGRES_HOST", "localhost");
    var port = Get("POSTGRES_PORT", "5432");
    var database = Get("POSTGRES_DB", "minipos");
    var user = Get("POSTGRES_USER", "postgres");
    var password = Get("POSTGRES_PASSWORD", "postgres");

    return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
}
