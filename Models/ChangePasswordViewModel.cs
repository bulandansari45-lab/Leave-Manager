using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Models;

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8), RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s]).+$", ErrorMessage = "Password needs an uppercase letter, a number, and a symbol.")] public string NewPassword { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
}
