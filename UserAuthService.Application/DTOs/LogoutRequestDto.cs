namespace UserAuthService.Application.DTOs
{
    public class LogoutRequestDto
    {
        public required string RefreshToken { get; set; }
    }
}