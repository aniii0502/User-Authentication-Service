namespace UserAuthService.Application.DTOs
{
    public class UserResponseDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public string? FullName { get; set; }
        public string? Token { get; set; }  // JWT token
    }
}