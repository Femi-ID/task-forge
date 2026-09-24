using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Dtos;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto registerDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await authService.RegisterAsync(registerDto);
            return response.Succeeded ? Ok(response.Data) : BadRequest(response.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var response = await authService.LoginAsync(loginDto);
            return response.Succeeded ? Ok(response.Data) : Unauthorized(response.Errors);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> GenerateRefreshToken([FromBody] RefreshTokenDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await authService.RefreshTokenAsync(dto.RefreshToken);
            return response.Succeeded ? Ok(response.Data) : Unauthorized(response.Errors);
        }

        [Authorize]
        [HttpGet("test")]
        public async  Task<IActionResult> Test()
        {
            return Ok("you are authorized!");
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); //NameIdentifier is the userId 
            var response = await authService.LogoutAsync(userId);
            return response.Succeeded ? NoContent(): BadRequest(response.Errors);
        }
    }
}