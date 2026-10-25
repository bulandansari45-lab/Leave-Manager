using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    [StringLength(100)] public string Designation { get; set; } = "";
    [StringLength(300)] public string? ProfilePhotoPath { get; set; }
    [Range(0, 366)] public int AnnualLeaveAllowanceDays { get; set; } = 20;
    public bool IsActive { get; set; } = true;
}