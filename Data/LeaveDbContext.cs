using LeaveManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Data;

public class LeaveDbContext(DbContextOptions<LeaveDbContext> options) : DbContext(options)
{
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.Property(x => x.EmployeeName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(160).IsRequired();
            entity.Property(x => x.LeaveType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.DecisionByName).HasMaxLength(100);
            entity.Property(x => x.DecisionByEmail).HasMaxLength(160);
            entity.Property(x => x.DecisionEmailStatus).HasMaxLength(20).HasDefaultValue("Unknown").IsRequired();
            entity.Property(x => x.RejectionReason).HasMaxLength(500);
            entity.Property(x => x.MoreInfoRequest).HasMaxLength(500);
            entity.Property(x => x.EmployeeResponse).HasMaxLength(500);
        });
    }
}
