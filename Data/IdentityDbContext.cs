using LeaveManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<AccountStatusChange> AccountStatusChanges => Set<AccountStatusChange>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>()
            .Property(user => user.IsActive)
            .HasDefaultValue(true);

        builder.Entity<AccountStatusChange>(entity =>
        {
            entity.HasKey(change => change.Id);
            entity.HasIndex(change => change.ChangedAtUtc);
            entity.Property(change => change.EmployeeUserId).HasMaxLength(450).IsRequired();
            entity.Property(change => change.EmployeeName).HasMaxLength(100).IsRequired();
            entity.Property(change => change.EmployeeEmail).HasMaxLength(256).IsRequired();
            entity.Property(change => change.ChangedByName).HasMaxLength(100).IsRequired();
            entity.Property(change => change.ChangedByEmail).HasMaxLength(256).IsRequired();
        });
    }
}
