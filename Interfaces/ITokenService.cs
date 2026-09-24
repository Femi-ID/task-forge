using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Models;

namespace api.Interfaces
{
    public interface ITokenService
    {
        string CreateAccessToken(AppUser user, IList<string> roles);
        (string RawToken, string HashedToken) GenerateRefreshToken();
        string HashToken(string rawToken);
        // Task<string?> CreateAndSaveRefreshTokenHashAsync(AppUser user);
    }
}