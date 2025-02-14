using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AceJobAgency3.Data;
using AceJobAgency3.Services;  // include the email service namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency3.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.Email == Input.Email);
            if (member == null)
            {
                // Do not reveal that the email is not registered.
                StatusMessage = "If an account with that email exists, a reset link has been sent.";
                return Page();
            }

            // Generate a token (using a GUID here)
            var token = Guid.NewGuid().ToString();
            // Set expiry to 1 hour from now
            var expiry = DateTime.UtcNow.AddHours(1);

            // Remove any existing tokens for this member
            var existingTokens = _context.PasswordResetTokens.Where(t => t.MemberId == member.Id);
            _context.PasswordResetTokens.RemoveRange(existingTokens);

            // Save the new token record
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                MemberId = member.Id,
                Token = token,
                Expiry = expiry
            });

            await _context.SaveChangesAsync();

            // Build the reset link
            var resetLink = Url.Page("ResetPassword", null, new { token = token }, Request.Scheme);

            // Send the email
            var subject = "Password Reset Request";
            var message = $"Please reset your password by clicking <a href='{resetLink}'>here</a>.";

            await _emailSender.SendEmailAsync(Input.Email, subject, message);

            StatusMessage = "A reset link has been sent to your email.";
            return Page();
        }
    }
}
