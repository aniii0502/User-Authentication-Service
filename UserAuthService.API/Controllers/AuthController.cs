using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UserAuthService.Application.DTOs;
using UserAuthService.Application.Interfaces;
using UserAuthService.Domain.Entities;

namespace UserAuthService.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshRepo;

        public AuthController(
            IAuthService authService,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshRepo) // corrected parameter name
        {
            _authService = authService;
            _userRepository = userRepository;
            _refreshRepo = refreshRepo; // assign correctly
        }

        // -------------------
        // Register
        // -------------------
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
        {
            var user = await _authService.RegisterAsync(dto.FullName, dto.Email, dto.Password);
            return Created("", new
            {
                user.Id,
                user.Email,
                user.FullName
            });
        }

        // -------------------
        // Login
        // -------------------
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            var (accessToken, refreshToken) = await _authService.LoginAsync(dto.Email, dto.Password);

            return Ok(new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        // -------------------
        // Refresh token
        // -------------------
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest dto)
        {
            var accessToken = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(new { AccessToken = accessToken });
        }

        // -------------------
        // Logout
        // -------------------
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
        {
            var tokenEntry = await _refreshRepo.GetByTokenAsync(dto.RefreshToken);
            if (tokenEntry != null)
                await _refreshRepo.DeleteAsync(tokenEntry);

            return Ok(new { message = "Logged out successfully." });
        }

        // -------------------
        // Get current authenticated user
        // -------------------
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userIdClaim));
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                Roles = user.Roles?.Select(r => r.Name)
            });
        }

        // -------------------
        // Forgot password
        // -------------------
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest dto)
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        // -------------------
        // Reset password
        // -------------------
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest dto)
        {
            await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
            return Ok(new { message = "Password reset successful." });
        }
    }
}
