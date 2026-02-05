using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using TestAPI.Configuration;
using TestAPI.Models;

namespace TestAPI.Services;

public interface IRefreshTokenService
{
    RefreshTokenInfo CreateRefreshToken(string username);
    RefreshTokenInfo? ValidateRefreshToken(string token);
    void RevokeRefreshToken(string token);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ConcurrentDictionary<string, RefreshTokenInfo> _tokens = new();
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    public RefreshTokenInfo CreateRefreshToken(string username)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var info = new RefreshTokenInfo
        {
            Token = refreshToken,
            Username = username,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            RefreshExpiresAt = refreshTokenExpiration
        };

        _tokens[refreshToken] = info;
        return info;
    }

    public RefreshTokenInfo? ValidateRefreshToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        if (_tokens.TryGetValue(token, out var info))
        {
            if (info.RefreshExpiresAt < DateTime.UtcNow)
            {
                _tokens.TryRemove(token, out _);
                return null;
            }

            return info;
        }

        return null;
    }

    public void RevokeRefreshToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _tokens.TryRemove(token, out _);
    }
}
