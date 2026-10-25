using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Models;

public class AccountStatusChange
{
    public int Id { get; set; }
    [Required, StringLength(450)] public string EmployeeUserId { get; set; } = string.Empty;
    [Required, StringLength(100)] public string EmployeeName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string EmployeeEmail { get; set; } = string.Empty;
    [Required, StringLength(100)] public string ChangedByName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string ChangedByEmail { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
