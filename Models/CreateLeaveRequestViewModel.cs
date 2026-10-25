using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Models;

public class CreateLeaveRequestViewModel
{
    [Required, StringLength(40)] public string LeaveType { get; set; } = "Annual leave";
    [Required, DataType(DataType.Date)] public DateTime? StartDate { get; set; }
    [Required, DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
}
