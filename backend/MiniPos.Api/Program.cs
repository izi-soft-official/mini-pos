using System.Text;
using System.Threading;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MiniPos.Api.Data;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;
using MiniPos.Api.Repositories;
using MiniPos.Api.Services;



var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JwtSettings:Secret is missing from configuration.");
var key = Encoding.UTF8.GetBytes(secretKey);

var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConn))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required in configuration.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConn));


builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IAuthRepo, AuthRepo>();
builder.Services.AddScoped<ITokenService, TokenService>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuerSigningKey = true
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
                     ?? [];

builder.Services.AddCors(options =>
  options.AddDefaultPolicy(policy => policy
      .WithOrigins(allowedOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod()));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();

        // Use Migrate() instead of EnsureCreated() when using migrations
        db.Database.Migrate();

        if (!db.Users.Any(u => u.Username == "admin"))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "admino",
                FullName = "Testo",
                Role = "Admin",
                IsActive = true
            };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admino123");

            db.Users.Add(user);
            db.SaveChanges();

            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Default test user 'admin' seeded successfully.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}


app.Run();
