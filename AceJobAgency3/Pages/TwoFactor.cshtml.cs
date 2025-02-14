using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using AceJobAgency3.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency3.Pages
{
    public class TwoFactorModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TwoFactorModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string StatusMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int MemberId { get; set; }


        public class InputModel
        {
            [Required]
            [Display(Name = "2FA Code")]
            public string Code { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (MemberId == 0)
            {
                return RedirectToPage("Login");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (MemberId == 0)
            {
                return RedirectToPage("Login");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == MemberId);
            if (member == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            // Check that the 2FA code is still valid
            if (member.TwoFactorCodeExpiry == null || member.TwoFactorCodeExpiry < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "The two-factor code has expired.");
                return Page();
            }

            if (member.TwoFactorCode != Input.Code)
            {
                ModelState.AddModelError(string.Empty, "Invalid two-factor code.");
                return Page();
            }

            // Clear the 2FA code from the member record
            member.TwoFactorCode = null;
            member.TwoFactorCodeExpiry = null;

            // Generate a new session token (for detecting multiple logins)
            member.CurrentSessionToken = Guid.NewGuid().ToString();
            await _context.SaveChangesAsync();

            // Create authentication claims and sign in the user
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
                new Claim(ClaimTypes.Email, member.Email),
                new Claim("SessionToken", member.CurrentSessionToken ?? "")

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

            return RedirectToPage("Index");
        }
    }
}
