namespace UserAuthService.Application.Config
{
    public class JwtOptions
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; set; } = 30; // default 30 mins
        public int RefreshTokenExpiryDays { get; set; } = 7;     // default 7 days
    }
}
