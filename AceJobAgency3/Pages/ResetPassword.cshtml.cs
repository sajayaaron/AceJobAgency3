using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AceJobAgency3.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency3.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Member> _passwordHasher;

        public ResetPasswordModel(ApplicationDbContext context, IPasswordHasher<Member> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string StatusMessage { get; set; }

        public class InputModel
        {
            public string Token { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            public string ConfirmNewPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                StatusMessage = "Invalid password reset token.";
                return Page();
            }

            // Check that the token exists and hasn't expired
            var resetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (resetToken == null || resetToken.Expiry < DateTime.UtcNow)
            {
                StatusMessage = "The password reset token is invalid or has expired.";
                return Page();
            }

            Input = new InputModel { Token = token };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var resetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == Input.Token);
            if (resetToken == null || resetToken.Expiry < DateTime.UtcNow)
            {
                StatusMessage = "The password reset token is invalid or has expired.";
                return Page();
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == resetToken.MemberId);
            if (member == null)
            {
                StatusMessage = "User not found.";
                return Page();
            }

            // Check minimum password age (5 minutes)
            if (DateTime.UtcNow - member.LastPasswordChangedDate < TimeSpan.FromMinutes(5))
            {
                ModelState.AddModelError(string.Empty, "You cannot change your password within 5 minutes of the last change.");
                return Page();
            }

            // Validate new password complexity
            if (!IsValidPassword(Input.NewPassword))
            {
                ModelState.AddModelError("Input.NewPassword", "Password must be at least 12 characters long and include uppercase, lowercase, numbers, and special characters.");
                return Page();
            }

            // Enforce password history: do not allow reuse of last two passwords
            var recentHistories = await _context.PasswordHistories
                .Where(ph => ph.MemberId == member.Id)
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

            // Save the current password in history before updating
            var passwordHistory = new PasswordHistory
            {
                MemberId = member.Id,
                PasswordHash = member.PasswordHash,
                ChangedDate = DateTime.UtcNow
            };
            _context.PasswordHistories.Add(passwordHistory);

            // Update the member's password
            member.PasswordHash = _passwordHasher.HashPassword(member, Input.NewPassword);
            member.LastPasswordChangedDate = DateTime.UtcNow;


            // Remove the used reset token
            _context.PasswordResetTokens.Remove(resetToken);

            await _context.SaveChangesAsync();

            StatusMessage = "Your password has been reset successfully. You can now log in with your new password.";
            return RedirectToPage("Login");
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
