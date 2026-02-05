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

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithOpenApi()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshToken)
            .WithName("RefreshToken")
            .WithOpenApi()
            .Produces<ApiResponse<RefreshTokenResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        IUserService userService,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        ILogger<AuthModule> logger)
    {
        logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var user = await userService.FindByUsernameAsync(request.Username);
        if (user is null || !userService.VerifyPassword(user, request.Password))
        {
            logger.LogWarning("Failed login attempt for user: {Username}", request.Username);

            var unauthorizedResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid username or password"
            };

            return Results.Json(unauthorizedResponse, statusCode: StatusCodes.Status401Unauthorized);
        }

        var token = jwtService.GenerateToken(user.Username, user.Roles);
        var refreshTokenInfo = refreshTokenService.CreateRefreshToken(user.Username);

        var response = new ApiResponse<LoginResponse>
        {
            Success = true,
            Data = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshTokenInfo.Token,
                ExpiresAt = refreshTokenInfo.ExpiresAt,
                RefreshTokenExpiresAt = refreshTokenInfo.RefreshExpiresAt,
                Roles = user.Roles.ToList()
            },
            Message = "Login successful"
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        IRefreshTokenService refreshTokenService,
        IUserService userService,
        IJwtService jwtService,
        ILogger<AuthModule> logger)
    {
        logger.LogInformation("Refreshing token");

        var storedToken = refreshTokenService.ValidateRefreshToken(request.RefreshToken);
        if (storedToken is null)
        {
            logger.LogWarning("Invalid or expired refresh token");
            var unauthorizedResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            };

            return Results.Json(unauthorizedResponse, statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await userService.FindByUsernameAsync(storedToken.Username);
        if (user is null)
        {
            refreshTokenService.RevokeRefreshToken(request.RefreshToken);
            var unauthorizedResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "User not found"
            };

            return Results.Json(unauthorizedResponse, statusCode: StatusCodes.Status401Unauthorized);
        }

        refreshTokenService.RevokeRefreshToken(request.RefreshToken);

        var token = jwtService.GenerateToken(user.Username, user.Roles);
        var refreshTokenInfo = refreshTokenService.CreateRefreshToken(user.Username);

        var response = new ApiResponse<RefreshTokenResponse>
        {
            Success = true,
            Data = new RefreshTokenResponse
            {
                Token = token,
                RefreshToken = refreshTokenInfo.Token,
                ExpiresAt = refreshTokenInfo.ExpiresAt,
                RefreshTokenExpiresAt = refreshTokenInfo.RefreshExpiresAt,
                Roles = user.Roles.ToList()
            },
            Message = "Token refreshed successfully"
        };

        return Results.Ok(response);
    }
}
