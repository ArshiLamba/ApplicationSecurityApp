namespace ApplicationSecurityApp.ViewModels
{
    public class HomeViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string DecryptedNRIC { get; set; } // Decrypted NRIC for display
        public string Gender { get; set; } // Added Gender
        public DateTime DateOfBirth { get; set; } // Added Date of Birth
    }
}
