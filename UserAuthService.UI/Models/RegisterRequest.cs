namespace UserAuthService.UI.Models
{
    public class RegisterRequest
    {
        public string? FullName { get; set; } 
        public required  string UserName{get;set; }
        public required  string Email { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
