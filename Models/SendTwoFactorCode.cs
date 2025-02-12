using System.ComponentModel.DataAnnotations;

namespace ApplicationSecurityApp.Models
{
    public class SendTwoFactorCode
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } // Hidden field to keep track of user

    }

}
