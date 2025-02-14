using System;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgency3.Data
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        public int MemberId { get; set; }

        [Required]
        public string Token { get; set; }

        public DateTime Expiry { get; set; }
    }
}
