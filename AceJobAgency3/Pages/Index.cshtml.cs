using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AceJobAgency3.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency3.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;

        public IndexModel(ApplicationDbContext context, IDataProtectionProvider provider)
        {
            _context = context;
            _protector = provider.CreateProtector("NRICProtector");
        }

        public Member Member { get; set; }
        public string DecryptedNRIC { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Redirect to login if not authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("Login");
            }

            // Get the user ID from claims
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            Member = await _context.Members.FirstOrDefaultAsync(m => m.Id == userId);
            if (Member == null)
            {
                return RedirectToPage("Login");
            }

            // Validate session token (to detect multiple logins)
            var sessionTokenClaim = User.FindFirst("SessionToken")?.Value;
            if (Member.CurrentSessionToken != sessionTokenClaim)
            {
                // Session token mismatch – possibly logged in from another device
                return RedirectToPage("Login");
            }

            // Maximum password age: if older than 90 days, force password change.
            if (DateTime.UtcNow - Member.LastPasswordChangedDate > TimeSpan.FromDays(90))
            {
                TempData["ForcePasswordChange"] = "Your password has expired. Please change your password.";
                return RedirectToPage("ChangePassword");
            }

            // Decrypt the NRIC
            try
            {
                DecryptedNRIC = _protector.Unprotect(Member.EncryptedNRIC);
            }
            catch
            {
                DecryptedNRIC = "Error decrypting NRIC";
            }

            return Page();
        }
    }
}

