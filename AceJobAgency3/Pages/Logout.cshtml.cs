using System.Security.Claims;
using System.Threading.Tasks;
using AceJobAgency3.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AceJobAgency3.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LogoutModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Write audit log for logout
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int memberId))
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    MemberId = memberId,
                    Activity = "Logged out",
                    ActivityDate = System.DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync();
            return RedirectToPage("Login");
        }
    }
}

