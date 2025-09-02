namespace UserAuthService.Domain.Entities
{
	public class PasswordResetToken
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string TokenHash { get; set; } = string.Empty; // store hashed token
		public Guid UserId { get; set; }
		public User User { get; set; } = null!;
		public DateTime ExpiresAt { get; set; }
		public bool IsUsed { get; set; } = false;
	}
}