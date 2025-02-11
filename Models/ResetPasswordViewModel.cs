using System.ComponentModel.DataAnnotations;

namespace ApplicationSecurityApp.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } // Reset token sent via email/SMS

        [Required]
        [EmailAddress]
        public string Email { get; set; } // User's email for verification

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
