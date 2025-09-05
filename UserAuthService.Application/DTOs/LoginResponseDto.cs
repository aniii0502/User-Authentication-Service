namespace UserAuthService.Application.DTOs
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public required string RefreshToken { get; set; }
    }
}
