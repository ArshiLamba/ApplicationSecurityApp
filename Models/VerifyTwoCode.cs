using System.ComponentModel.DataAnnotations;

namespace ApplicationSecurityApp.Models
{
    public class VerifyTwoCode
    {
        [Required]
        [Display(Name = "One-Time Password (OTP)")]
        public string OtpCode { get; set; }
    }
}
