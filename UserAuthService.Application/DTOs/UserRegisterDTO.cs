namespace UserAuthService.Application.DTOs
{
    public class UserRegisterDto
    {
        public string? FullName { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
