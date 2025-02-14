using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using AceJobAgency3.Data;
using AceJobAgency3.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency3.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;
        private readonly IPasswordHasher<Member> _passwordHasher;
        private readonly IRecaptchaService _recaptchaService;


        public RegisterModel(ApplicationDbContext context, IDataProtectionProvider provider, IPasswordHasher<Member> passwordHasher, IRecaptchaService recaptchaService)
        {
            _context = context;
            _protector = provider.CreateProtector("NRICProtector");
            _passwordHasher = passwordHasher;
            _recaptchaService = recaptchaService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        // Bind the reCAPTCHA token from the form
        [BindProperty]
        public string RecaptchaToken { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required]
            public string Gender { get; set; }

            [Required]
            [Display(Name = "NRIC")]
            public string NRIC { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email Address")]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime DateOfBirth { get; set; }

            [Display(Name = "Resume")]
            public IFormFile Resume { get; set; }

            [Display(Name = "Who Am I")]
            public string WhoAmI { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validate reCAPTCHA token
            bool recaptchaValid = await _recaptchaService.ValidateTokenAsync(RecaptchaToken);
            if (!recaptchaValid)
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check for duplicate email
            var existingMember = await _context.Members.FirstOrDefaultAsync(m => m.Email == Input.Email);
            if (existingMember != null)
            {
                ModelState.AddModelError("Input.Email", "Email already registered.");
                return Page();
            }

            // Validate password complexity
            if (!IsValidPassword(Input.Password))
            {
                ModelState.AddModelError("Input.Password", "Password must be at least 12 characters long and include uppercase, lowercase, numbers, and special characters.");
                return Page();
            }

            // Process resume file upload if provided
            string resumePath = null;
            if (Input.Resume != null)
            {
                var extension = Path.GetExtension(Input.Resume.FileName).ToLower();
                if (extension != ".pdf" && extension != ".docx")
                {
                    ModelState.AddModelError("Input.Resume", "Only PDF or DOCX files are allowed.");
                    return Page();
                }

                // Save the file to wwwroot/resumes
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resumes");
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }
                var fileName = Guid.NewGuid() + extension;
                var filePath = Path.Combine(uploads, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.Resume.CopyToAsync(fileStream);
                }
                resumePath = "/resumes/" + fileName;
            }

            // Create the member instance
            var member = new Member
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Gender = Input.Gender,
                EncryptedNRIC = _protector.Protect(Input.NRIC),
                Email = Input.Email,
                DateOfBirth = Input.DateOfBirth,
                ResumePath = resumePath,
                WhoAmI = Input.WhoAmI,
                FailedLoginAttempts = 0,
                CurrentSessionToken = string.Empty, // or null if allowed
                LastPasswordChangedDate = DateTime.UtcNow
            };

            // Hash the password
            member.PasswordHash = _passwordHasher.HashPassword(member, Input.Password);

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            // Write an audit log entry for registration
            _context.AuditLogs.Add(new AuditLog
            {
                MemberId = member.Id,
                Activity = "Registered",
                ActivityDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Redirect to Login after successful registration
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

