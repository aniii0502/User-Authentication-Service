namespace UserAuthService.UI.Models
{
    public class UserResponse
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public string? FullName { get; set; }   
        public string? Token { get; set; }      
    }
}
