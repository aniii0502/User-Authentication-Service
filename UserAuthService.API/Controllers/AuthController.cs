using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
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
        private readonly IMapper _mapper;
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshRepo;

        public AuthController(
            IMapper mapper,
            IAuthService authService,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshRepo)
        {
            _mapper = mapper;
            _authService = authService;
            _userRepository = userRepository;
            _refreshRepo = refreshRepo;
        }

        // ------------------- Register -------------------
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match." });

            try
            {
                var dto = new UserRegisterDto
                {
                    FullName = request.FullName,
                    Username = request.UserName,
                    Email = request.Email,
                    Password = request.Password
                };

                var user = await _authService.RegisterAsync(dto);

                var response = new
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    UserName = dto.Username,
                    Token = user.Token
                };

                return Created("", response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ------------------- Login -------------------
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var dto = _mapper.Map<UserLoginDto>(request);
                var result = await _authService.LoginAsync(dto);
                var response = _mapper.Map<UserResponseDto>(result);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ------------------- Refresh Token -------------------
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new { message = "Refresh token is required." });

            try
            {
                var tokens = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(new { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ------------------- Logout -------------------
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new { message = "Refresh token is required." });

            var tokenEntry = await _refreshRepo.GetByTokenAsync(request.RefreshToken);
            if (tokenEntry != null)
                await _refreshRepo.DeleteAsync(tokenEntry);

            return Ok(new { message = "Logged out successfully." });
        }

        // ------------------- Get Current User -------------------
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

            var response = _mapper.Map<UserResponseDto>(user);
            return Ok(response);
        }

        // ------------------- Forgot Password -------------------
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(new { message = "Email is required." });

            try
            {
                var dto = _mapper.Map<ForgotPasswordDto>(request);
                await _authService.ForgotPasswordAsync(dto.Email);
                return Ok(new { message = "If the email exists, a reset link has been sent." });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Failed to send reset email. Check your email configuration." });
            }
        }

        // ------------------- Reset Password -------------------
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
                return BadRequest(new { message = "Token and new password are required." });

            try
            {
                var dto = _mapper.Map<ResetPasswordDto>(request);
                await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
                return Ok(new { message = "Password reset successful." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ------------------- Health Check -------------------
        [HttpGet("health")]
        public IActionResult Health() => Ok("UserAuthService is running 🚀");
    }
}
