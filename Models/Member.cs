using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApplicationSecurityApp.Models
{
    public class Member
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string EncryptedNRIC { get; set; } // Already encrypted in ViewModel

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; } // Hashed Password stored in DB

        [Required]
        public DateTime DateOfBirth { get; set; }

        public string ResumeFilePath { get; set; } // Store only file path

        [Required]
        public string WhoAmI { get; set; } // Store description

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Auto timestamp

        public int FailedLoginAttempts { get; set; } = 0; // Track failed logins
        public bool IsLockedOut { get; set; } = false; // Account lockout flag
        public DateTime? LastFailedLogin { get; set; } // Track last failed login time
        public DateTime? LastPasswordChange { get; set; } // Track password changes

        public bool RequiresPasswordChange { get; set; } = false;

        [NotMapped]
        public List<string> PasswordHistory { get; set; } = new List<string>();

        public bool IsLoggedIn { get; set; } = false; // Track multiple logins
        public DateTime? LockoutEnd { get; set; }

        public string? SessionId { get; set; }

        public string? ResetToken { get; set; } // Store reset token
        public DateTime? ResetTokenExpiry { get; set; } // Expiry time for token

        public bool Is2FAEnabled { get; set; } =true;
        public string? TwoFactorSecret { get; set; } // For Authenticator App
        public string? TwoFactorCode { get; set; } // For SMS-based 2FA
        public DateTime? TwoFactorExpiry { get; set; } // Expiry time for code
    }

}

