using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Models;

public class ProfileViewModel
{
    [Required, StringLength(100)] public string DisplayName { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [StringLength(100)] public string Designation { get; set; } = "";
    public string? ProfilePhotoPath { get; set; }
    public IFormFile? Photo { get; set; }
}