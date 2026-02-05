using Carter;
using Microsoft.AspNetCore.Mvc;
using TestAPI.Models;
using TestAPI.Services;

namespace TestAPI.Modules;

public class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AllowAnonymous();

        // POST: Login
        group.MapPost("/login", Login)
            .WithName("Login")
            .WithOpenApi()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        IJwtService jwtService,
        ILogger<AuthModule> logger)
    {
        logger.LogInformation("Login attempt for user: {Username}", request.Username);

        // Simple authentication - In production, use a proper user management system
        // This is just for demonstration purposes
        if (request.Username == "admin" && request.Password == "Admin123!")
        {
            var token = jwtService.GenerateToken(request.Username);
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            logger.LogInformation("User {Username} logged in successfully", request.Username);

            var response = new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = new LoginResponse
                {
                    Token = token,
                    ExpiresAt = expiresAt
                },
                Message = "Login successful"
            };

            return Results.Ok(response);
        }

        logger.LogWarning("Failed login attempt for user: {Username}", request.Username);

        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = "Invalid username or password"
        };

        return Results.Unauthorized();
    }
}
