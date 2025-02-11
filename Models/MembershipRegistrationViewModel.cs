using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ApplicationSecurityApp.Security;
using System.Net; // Import EncryptionHelper

namespace AceJobAgency.ViewModels
{
    public class MembershipRegistrationViewModel
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string NRIC { get; set; } // This will be encrypted before saving

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [NotMapped] // Prevent EF from storing plain text password
        [Required]
        [DataType(DataType.Password)]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; }

        [NotMapped]
        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [NotMapped] // Prevent EF from mapping IFormFile
        public IFormFile Resume { get; set; }

        [Required]
        public string WhoAmI { get; set; }

        // 🔒 Hash the password before storing it
        public string HashPassword()
        {
            var passwordHasher = new PasswordHasher<MembershipRegistrationViewModel>();
            return passwordHasher.HashPassword(this, Password);
        }
        // 🛡️ Sanitize inputs to prevent XSS attacks
        public void SanitizeInputs()
        {
            this.FirstName = WebUtility.HtmlEncode(this.FirstName);
            this.LastName = WebUtility.HtmlEncode(this.LastName);
            this.NRIC = WebUtility.HtmlEncode(this.NRIC);
            this.Email = WebUtility.HtmlEncode(this.Email);
            this.WhoAmI = WebUtility.HtmlEncode(this.WhoAmI);
        }
    }
}
