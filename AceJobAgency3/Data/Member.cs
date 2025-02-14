using System;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgency3.Data
{
    public class Member
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string EncryptedNRIC { get; set; } // Encrypted using Data Protection

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        // Store the resume file path (file saved in wwwroot/resumes)
        public string ResumePath { get; set; }

        // "Who Am I" – free text
        public string WhoAmI { get; set; }

        // For login rate limiting/account lockout
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }

        // Session token to detect multiple logins
        public string? CurrentSessionToken { get; set; }

        // New property to store when the password was last changed.
        public DateTime LastPasswordChangedDate { get; set; }

        public string? TwoFactorCode { get; set; }
        public DateTime? TwoFactorCodeExpiry { get; set; }

    }
}
