using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ApplicationSecurityApp.Security; // Import EncryptionHelper

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
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
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
    }
}
