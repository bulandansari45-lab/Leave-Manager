namespace LeaveManagement.Models;

public class EmailSettings
{
    public bool Enabled { get; set; }
    public string GmailAddress { get; set; } = "";
    public string AppPassword { get; set; } = "";
}
