public class EmailOptions
{
    public string ApiKey { get; set; } = string.Empty; // SendGrid API key or SMTP credentials
    public string FromEmail { get; set; } = "no-reply@yourapp.com";
    public string FromName { get; set; } = "YourApp";
}
