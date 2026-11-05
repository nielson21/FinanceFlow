using FinanceFlow.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<FinanceRequest> FinanceRequests { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApprovalHistory>()
                .HasOne(x => x.ActionByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinanceRequest>()
                .HasIndex(x => x.ReferenceNo)
                .IsUnique();

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                builder.Entity<FinanceRequest>()
                    .Property(x => x.RowVersion)
                    .ValueGeneratedNever();
            }
        }
    }
}
