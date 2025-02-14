using System;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgency3.Data
{
    public class PasswordHistory
    {
        public int Id { get; set; }

        public int MemberId { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime ChangedDate { get; set; }
    }
}
