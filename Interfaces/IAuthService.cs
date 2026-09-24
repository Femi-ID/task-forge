using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Models;

namespace api.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponseDto>> LoginAsync(LoginUserDto loginDto);
        Task<Result<AuthResponseDto>> RegisterAsync(RegisterUserDto registerDto);
        Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
        // Task<Result<AuthResponseDto?>> GenerateRefreshTokenAsync(RefreshTokenDto refreshDto);
        Task<Result<bool>> LogoutAsync(Guid userId);
    }
}