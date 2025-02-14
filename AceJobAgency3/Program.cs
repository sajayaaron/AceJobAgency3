using AceJobAgency3.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AceJobAgency3.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();



// Add Razor Pages
builder.Services.AddRazorPages();

// Configure EF Core with your connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Add Data Protection (for encrypting the NRIC)
builder.Services.AddDataProtection();

// Configure Cookie Authentication (session management)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // session timeout
    });

// Add the password hasher (from Microsoft.AspNetCore.Identity)
builder.Services.AddScoped<IPasswordHasher<Member>, PasswordHasher<Member>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Optionally, handle 404 errors
    app.UseStatusCodePagesWithReExecute("/NotFound");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

