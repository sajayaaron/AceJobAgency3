using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AceJobAgency3.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency3.Pages
{
    public class ChangePasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Member> _passwordHasher;

        public ChangePasswordModel(ApplicationDbContext context, IPasswordHasher<Member> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Old Password")]
            public string OldPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation do not match.")]
            public string ConfirmNewPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Get current member id from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return RedirectToPage("Login");
            }

            int memberId = int.Parse(userIdClaim.Value);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
            if (member == null)
            {
                return RedirectToPage("Login");
            }

            // Check minimum password age (5 minutes)
            if (DateTime.UtcNow - member.LastPasswordChangedDate < TimeSpan.FromMinutes(5))
            {
                ModelState.AddModelError(string.Empty, "You cannot change your password within 5 minutes of the last change.");
                return Page();
            }

            // Verify the old password
            var verifyOld = _passwordHasher.VerifyHashedPassword(member, member.PasswordHash, Input.OldPassword);
            if (verifyOld == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Old password is incorrect.");
                return Page();
            }

            // Check that new password is different from the old password
            if (Input.OldPassword == Input.NewPassword)
            {
                ModelState.AddModelError(string.Empty, "New password must be different from the old password.");
                return Page();
            }

            // Validate new password complexity
            if (!IsValidPassword(Input.NewPassword))
            {
                ModelState.AddModelError("Input.NewPassword", "Password must be at least 12 characters long and include uppercase, lowercase, numbers, and special characters.");
                return Page();
            }

            // Enforce password history: ensure the new password is not equal to any of the last two used
            var recentHistories = await _context.PasswordHistories
                .Where(ph => ph.MemberId == memberId)
                .OrderByDescending(ph => ph.ChangedDate)
                .Take(2)
                .ToListAsync();

            foreach (var history in recentHistories)
            {
                var historyResult = _passwordHasher.VerifyHashedPassword(member, history.PasswordHash, Input.NewPassword);
                if (historyResult == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError(string.Empty, "You cannot reuse one of your last two passwords.");
                    return Page();
                }
            }

            // All validations passed: store current password hash in history
            var passwordHistory = new PasswordHistory
            {
                MemberId = memberId,
                PasswordHash = member.PasswordHash,
                ChangedDate = DateTime.UtcNow
            };
            _context.PasswordHistories.Add(passwordHistory);

            // Update member's password hash with the new password
            member.PasswordHash = _passwordHasher.HashPassword(member, Input.NewPassword);
            member.LastPasswordChangedDate = DateTime.UtcNow;


            await _context.SaveChangesAsync();

            StatusMessage = "Your password has been changed successfully.";
            return RedirectToPage("Index");
        }

        private bool IsValidPassword(string password)
        {
            if (password.Length < 12)
                return false;
            bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;
            foreach (var ch in password)
            {
                if (char.IsUpper(ch)) hasUpper = true;
                else if (char.IsLower(ch)) hasLower = true;
                else if (char.IsDigit(ch)) hasDigit = true;
                else if (!char.IsLetterOrDigit(ch)) hasSpecial = true;
            }
            return hasUpper && hasLower && hasDigit && hasSpecial;
        }
    }
}

