using System.Text;
using System.Text.Encodings.Web;
using System.Threading.RateLimiting;
using Carter;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TestAPI.Authentication;
using TestAPI.Configuration;
using TestAPI.Data;
using TestAPI.Middleware;
using TestAPI.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting Student API application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    builder.Services.Configure<ApiKeySettings>(builder.Configuration.GetSection("ApiKeySettings"));

    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddCarter();

    builder.Services.AddSingleton<IInputSanitizer, InputSanitizer>();
    builder.Services.AddSingleton(_ => HtmlEncoder.Default);
    builder.Services.AddSingleton<IUserService, UserService>();
    builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
    builder.Services.AddScoped<IJwtService, JwtService>();

    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var identifier = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(identifier, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromSeconds(20),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.Headers["Retry-After"] = "20";
            await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
        };
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Combined";
        options.DefaultChallengeScheme = "Combined";
    })
    .AddPolicyScheme("Combined", "JWT or API Key", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.ContainsKey("X-Api-Key")
                ? "ApiKey"
                : JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings!.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("Authentication failed: {Message}", context.Exception?.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("Token validated for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
        options.AddPolicy("UserPolicy", policy => policy.RequireRole("Admin", "User"));
    });

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres");

    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseRateLimiter();

    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
        await next();
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseCors("DefaultPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapCarter();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied");
    }

    Log.Information("Student API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
