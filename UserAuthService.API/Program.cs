using System.Reflection;
using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserAuthService.Domain.Config;
using UserAuthService.Application.Interfaces;
using UserAuthService.Application.Services;
using UserAuthService.Infrastructure.Data;
using UserAuthService.Infrastructure.Repositories;
using UserAuthService.Infrastructure.Services;
using UserAuthService.Infrastructure.Config;
using UserAuthService.API.Mapping;



var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
builder.Services.AddAutoMapper(typeof(AuthMappingProfile));


// --------------------
// Secrets from appsettings.json
// --------------------
var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>()
                 ?? throw new Exception("JwtOptions section missing");
if (string.IsNullOrEmpty(jwtOptions.SecretKey))
    throw new Exception("JWT Secret is missing");

// --------------------
// Database
// --------------------
var connectionString = configuration.GetConnectionString("DefaultConnection")
                       ?? throw new Exception("DefaultConnection not set");

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// --------------------
// JWT Authentication
// --------------------
var key = Encoding.ASCII.GetBytes(jwtOptions.SecretKey);

builder.Services.Configure<JwtOptions>(options =>
{
    options.SecretKey = jwtOptions.SecretKey;
    options.Issuer = jwtOptions.Issuer ?? "MyAuthService";
    options.Audience = jwtOptions.Audience ?? "MyClientApp";
    options.AccessTokenExpiryMinutes = jwtOptions.AccessTokenExpiryMinutes > 0 ? jwtOptions.AccessTokenExpiryMinutes : 15;
    options.RefreshTokenExpiryDays = jwtOptions.RefreshTokenExpiryDays > 0 ? jwtOptions.RefreshTokenExpiryDays : 7;
});

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
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// --------------------
// SendGrid Email Service
// --------------------
//var sendGridOptions = configuration.GetSection("Email").Get<SendGridOptions>()
//                     ?? throw new Exception("Email section missing");

//builder.Services.AddSingleton(sendGridOptions);
builder.Services.AddScoped<IEmailService, MockEmailService>();

// --------------------
// Dependency Injection
// --------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<UserService>();

// --------------------
// Controllers & Swagger
// --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT with Bearer prefix, e.g. 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --------------------
// CORS
// --------------------
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --------------------
// Rate Limiting
// --------------------
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule { Endpoint = "*:/api/auth/login", Limit = 5, Period = "1m" },
        new RateLimitRule { Endpoint = "*:/api/auth/forgot-password", Limit = 3, Period = "1m" }
    };
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// --------------------
// Health Checks
// --------------------
builder.Services.AddHealthChecks()
       .AddDbContextCheck<AuthDbContext>("Database")
       .AddCheck<EmailHealthCheck>("EmailService");

// --------------------
// Build App
// --------------------
var app = builder.Build();

// --------------------
// Middleware
// --------------------
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseHttpsRedirection();
app.UseXContentTypeOptions();
app.UseXfo(options => options.Deny());
app.UseCsp(csp => csp
    .DefaultSources(s => s.Self())
    .ScriptSources(s => s.Self())
    .StyleSources(s => s.Self())
);

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseIpRateLimiting();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1"));
}

// --------------------
// Health endpoint
// --------------------
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                exception = e.Value.Exception?.Message,
                duration = e.Value.Duration.ToString()
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

// --------------------
// Map Controllers
// --------------------
app.MapControllers();

// --------------------
// Auto-migrate DB
// --------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
}

// --------------------
// Run App
// --------------------
app.Run();

public partial class Program { } 
// to make the implicit Program class public for integration testing
