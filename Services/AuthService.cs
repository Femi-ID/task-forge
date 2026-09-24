using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService) : IAuthService
    {
        public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterUserDto registerDto)
        {
            var user = new AppUser
            {
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                UserName = registerDto.Email
            };
            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                return Result<AuthResponseDto>.Failure(result.Errors.Select(e => e.Description));

            await userManager.AddToRoleAsync(user, "User");
            return Result<AuthResponseDto>.Success(await IssueTokensAsync(user));
        }

        public async Task<Result<AuthResponseDto>> LoginAsync(LoginUserDto loginDto)
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email.ToLower());
            if (user is null) return Result<AuthResponseDto>.Failure("Invalid email or password");

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
            if (!result.Succeeded) return Result<AuthResponseDto>.Failure("Invalid email or password");

            return Result<AuthResponseDto>.Success(await IssueTokensAsync(user));
        }

        public async Task<Result<bool>> LogoutAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null) return Result<bool>.Failure("User not found");

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await userManager.UpdateAsync(user);
            return Result<bool>.Success(true);
        }

        public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            var refreshTokenHash = tokenService.HashToken(refreshToken);
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenHash);

            if (user is null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Result<AuthResponseDto>.Failure("Invalid or expired refresh token");

            return Result<AuthResponseDto>.Success(await IssueTokensAsync(user));
        }

        private async Task<AuthResponseDto> IssueTokensAsync(AppUser user)
        {
            var roles = await userManager.GetRolesAsync(user);
            var accessToken = tokenService.CreateAccessToken(user, roles);
            var (rawRefreshToken, hashedRefreshToken) = tokenService.GenerateRefreshToken();

            user.RefreshToken = hashedRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime.Value,
                Email = user.Email!,
                FullName = user.FullName
            };
        }
    }
}