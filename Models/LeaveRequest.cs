using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Models;

public enum LeaveStatus { Pending, Approved, Rejected, NeedsMoreInfo }

public class LeaveRequest
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string EmployeeName { get; set; } = "";
    [Required, EmailAddress, StringLength(160)] public string Email { get; set; } = "";
    [Required] public string LeaveType { get; set; } = "Annual leave";
    [Required, DataType(DataType.Date)] public DateTime StartDate { get; set; }
    [Required, DataType(DataType.Date)] public DateTime EndDate { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string? DecisionByName { get; set; }
    [EmailAddress, StringLength(160)] public string? DecisionByEmail { get; set; }
    public DateTime? DecisionAt { get; set; }
    [StringLength(20)] public string DecisionEmailStatus { get; set; } = "Unknown";
    public DateTime? DecisionEmailSentAt { get; set; }
    [StringLength(500)] public string? RejectionReason { get; set; }
    [StringLength(500)] public string? MoreInfoRequest { get; set; }
    [StringLength(500)] public string? EmployeeResponse { get; set; }
    public int Days => Math.Max(0, (EndDate.Date - StartDate.Date).Days + 1);
}
