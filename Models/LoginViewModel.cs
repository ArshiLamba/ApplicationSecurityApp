using System.ComponentModel.DataAnnotations;

namespace ApplicationSecurityApp.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public string ReCaptchaToken { get; set; }
    }
}
