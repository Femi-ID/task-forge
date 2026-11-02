using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace api.Services
{
    public class TokenService(IConfiguration config) : ITokenService
    {
        public string CreateAccessToken(AppUser user, IList<string> roles)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:SigningKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var claims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new (JwtRegisteredClaimNames.Email, user.Email!),
                new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // Short-lived on purpose: the refresh token is what keeps people signed in, and a short life limits the
            // damage of a stolen access token. For local testing set "JWT:AccessTokenMinutes": 1440 in appsettings.Development.json.
            var lifetimeMinutes = config.GetValue<int?>("JWT:AccessTokenMinutes") ?? 120; //change to 15 later

            var token = new JwtSecurityToken(
                issuer: config["JWT:Issuer"],
                audience: config["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Not sure this is the correct implementation
        public (string RawToken, string HashedToken) GenerateRefreshToken()
        {
            var rawToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
            return (rawToken, HashToken(rawToken));
        }

        public string HashToken(string rawToken) => 
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        // private async Task<bool> ValidateRefreshTokenAsync(Guid userId, string userRequestRefreshToken)
        // {
        //     var user = await context.Users.FindAsync(userId);
        //     if (user is null || user.RefreshToken != userRequestRefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        //     {
        //         return false;
        //     }
        //     return true;
        // }

        // public async Task<string?> GenerateRefreshTokenAsync(AppUser user, string userRequestRefreshToken)
        // {
        //     var userDetails = await ValidateRefreshTokenAsync(user.Id, userRequestRefreshToken);
        //     if (userDetails is false) return null;

        //     //  generate and hash
        //     // var refreshToken = GenerateRefreshToken();
        //     // hash the refreshToken here

        //     // save the refreshTokenHash
        //     user.RefreshToken = refreshToken;
        //     user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        //     await userManager.UpdateAsync(user);
        //     return refreshToken;
        // }
    }
}