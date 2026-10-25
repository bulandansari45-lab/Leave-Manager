namespace LeaveManagement.Models;

public class LeaveCalendarViewModel
{
    public string MonthTitle { get; init; } = string.Empty;
    public bool OnlyMine { get; init; }
    public string TodaySummary { get; init; } = string.Empty;
    public int NextWeekPeopleCount { get; init; }
    public DateTime PreviousMonth { get; init; }
    public DateTime NextMonth { get; init; }
    public IReadOnlyList<LeaveCalendarDay> Days { get; init; } = [];
}

public class LeaveCalendarDay
{
    public DateTime Date { get; init; }
    public bool IsCurrentMonth { get; init; }
    public bool IsToday { get; init; }
    public IReadOnlyList<LeaveCalendarEntry> Entries { get; init; } = [];
}

public record LeaveCalendarEntry(string EmployeeName, string LeaveType);
