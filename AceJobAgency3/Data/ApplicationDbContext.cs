using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AceJobAgency3.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    }
}
