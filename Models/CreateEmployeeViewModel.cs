using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Models;

public class CreateEmployeeViewModel
{
    [Required, StringLength(100)] public string DisplayName { get; set; } = "";
    [Required, EmailAddress, StringLength(160)] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)] public string InitialPassword { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(InitialPassword))] public string ConfirmPassword { get; set; } = "";
}
