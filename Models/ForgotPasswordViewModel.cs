using System.ComponentModel.DataAnnotations;

namespace ApplicationSecurityApp.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
