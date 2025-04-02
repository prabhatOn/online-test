namespace OnlineAssessment.Web.Models
{
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Plain password input
        public string Role { get; set; } // Role required
    }
}
