using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using AceJobAgency3.Data;
using AceJobAgency3.Services; // For IEmailSender
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency3.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Member> _passwordHasher;
        private readonly IEmailSender _emailSender;


        public LoginModel(ApplicationDbContext context, IPasswordHasher<Member> passwordHasher, IEmailSender emailSender)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.Email == Input.Email);
            if (member == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // Check for account lockout
            if (member.LockoutEnd.HasValue && member.LockoutEnd.Value > DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Account is locked. Please try again later.");
                return Page();
            }

            // Verify the password
            var result = _passwordHasher.VerifyHashedPassword(member, member.PasswordHash, Input.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                member.FailedLoginAttempts++;
                if (member.FailedLoginAttempts >= 3)
                {
                    // Lock account for 15 minutes
                    member.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _context.SaveChangesAsync();
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // Successful login: reset failed attempts and lockout
            member.FailedLoginAttempts = 0;
            member.LockoutEnd = null;

            // Generate a 6-digit 2FA code.
            var twoFactorCode = new Random().Next(100000, 1000000).ToString();
            member.TwoFactorCode = twoFactorCode;
            member.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            // Send the 2FA code via email.
            var subject = "Your Two-Factor Authentication Code";
            var message = $"Your two-factor authentication code is: {twoFactorCode}";
            await _emailSender.SendEmailAsync(member.Email, subject, message);

            // Redirect to the TwoFactor page with the memberId as a query parameter.
            return RedirectToPage("TwoFactor", new { memberId = member.Id });


            // Generate a new session token (for detecting multiple logins)
            member.CurrentSessionToken = Guid.NewGuid().ToString();
            await _context.SaveChangesAsync();

            // Create authentication claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
                new Claim(ClaimTypes.Email, member.Email),
                new Claim("SessionToken", member.CurrentSessionToken)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Write audit log for login
            _context.AuditLogs.Add(new AuditLog
            {
                MemberId = member.Id,
                Activity = "Logged in",
                ActivityDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}
