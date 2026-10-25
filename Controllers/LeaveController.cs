using LeaveManagement.Models;
using LeaveManagement.Data;
using LeaveManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;

namespace LeaveManagement.Controllers;

[Authorize]
public class LeaveController(LeaveDbContext db, UserManager<ApplicationUser> users, IDecisionEmailSender decisionEmailSender, IOptions<EmailSettings> emailOptions) : Controller
{
    public async Task<IActionResult> Index(int page = 1, string status = "All", string? search = null, string sort = "newest")
    {
        var currentUser = await users.GetUserAsync(User);
        if (currentUser is null) return Challenge();
        var query = db.LeaveRequests.AsNoTracking();
        var isManager = User.IsInRole("Manager");
        ViewBag.IsManager = isManager;
        if (!isManager) query = query.Where(x => x.Email == currentUser.Email);
        ViewBag.CurrentUserName = currentUser.DisplayName;
        ViewBag.CurrentUserEmail = currentUser.Email;
        ViewBag.RequestError = TempData["RequestError"] as string;
        ViewBag.OpenRequestModal = (TempData["RequestModalOpen"] as string) == "true";
        ViewBag.RequestLeaveType = TempData["RequestLeaveType"] as string ?? "Annual leave";
        ViewBag.RequestStartDate = TempData["RequestStartDate"] as string ?? "";
        ViewBag.RequestEndDate = TempData["RequestEndDate"] as string ?? "";
        ViewBag.RequestReason = TempData["RequestReason"] as string ?? "";
        ViewBag.UndoRequestId = TempData["UndoRequestId"];
        ViewBag.UndoDecisionTicks = TempData["UndoDecisionTicks"];
        ViewBag.UndoExpiresAt = TempData["UndoExpiresAt"];

        var allRequests = await query.ToListAsync();
        var selectedStatus = status is "Pending" or "Approved" or "Rejected" ? status : "All";
        var searchText = search?.Trim() ?? "";
        var selectedSort = sort == "oldest" ? "oldest" : "newest";
        IEnumerable<LeaveRequest> filtered = allRequests;
        if (selectedStatus == "Pending") filtered = filtered.Where(x => x.Status is LeaveStatus.Pending or LeaveStatus.NeedsMoreInfo);
        else if (selectedStatus == "Approved") filtered = filtered.Where(x => x.Status == LeaveStatus.Approved);
        else if (selectedStatus == "Rejected") filtered = filtered.Where(x => x.Status == LeaveStatus.Rejected);
        if (searchText.Length > 0)
            filtered = filtered.Where(x => x.EmployeeName.Contains(searchText, StringComparison.OrdinalIgnoreCase) || x.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase) || x.LeaveType.Contains(searchText, StringComparison.OrdinalIgnoreCase) || x.Status.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase));
        filtered = selectedSort == "oldest"
            ? filtered.OrderBy(x => x.SubmittedAt).ThenBy(x => x.Id)
            : filtered.OrderByDescending(x => x.SubmittedAt).ThenByDescending(x => x.Id);

        const int pageSize = 10;
        var filteredList = filtered.ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredList.Count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        ViewBag.TotalRequests = allRequests.Count;
        ViewBag.PendingCount = allRequests.Count(x => x.Status == LeaveStatus.Pending);
        ViewBag.PendingFilterCount = allRequests.Count(x => x.Status is LeaveStatus.Pending or LeaveStatus.NeedsMoreInfo);
        ViewBag.ApprovedCount = allRequests.Count(x => x.Status == LeaveStatus.Approved);
        ViewBag.ApprovedDaysOff = allRequests.Where(x => x.Status == LeaveStatus.Approved).Sum(x => x.Days);
        ViewBag.RejectedCount = allRequests.Count(x => x.Status == LeaveStatus.Rejected);
        ViewBag.FilteredCount = filteredList.Count;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.CurrentStatus = selectedStatus;
        ViewBag.Search = searchText;
        ViewBag.Sort = selectedSort;
        ViewBag.PageSize = pageSize;

        if (!isManager)
        {
            var year = DateTime.Today.Year;
            var allowance = Math.Clamp(currentUser.AnnualLeaveAllowanceDays, 0, 366);
            var annualRequests = allRequests.Where(x => x.LeaveType == "Annual leave" && x.Status != LeaveStatus.Rejected);
            var approvedDays = annualRequests.Where(x => x.Status == LeaveStatus.Approved).Sum(x => DaysWithinYear(x, year));
            var pendingDays = annualRequests.Where(x => x.Status is LeaveStatus.Pending or LeaveStatus.NeedsMoreInfo).Sum(x => DaysWithinYear(x, year));
            ViewBag.BalanceYear = year;
            ViewBag.AnnualAllowanceDays = allowance;
            ViewBag.AnnualApprovedDays = approvedDays;
            ViewBag.AnnualPendingDays = pendingDays;
            ViewBag.AnnualRemainingDays = Math.Max(0, allowance - approvedDays - pendingDays);
        }
        return View(filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList());
    }
    [HttpGet]
    public async Task<IActionResult> Calendar(int? year, int? month, bool onlyMine = false)
    {
        var today = DateTime.Today;
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;
        if (selectedMonth is < 1 or > 12 || selectedYear is < 1 or > 9998)
            return BadRequest();

        var monthStart = new DateTime(selectedYear, selectedMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var currentUser = await users.GetUserAsync(User);
        var scopedToMine = onlyMine || !User.IsInRole("Manager");
        var leaveQuery = db.LeaveRequests.AsNoTracking().Where(item => item.Status == LeaveStatus.Approved && item.StartDate <= monthEnd && item.EndDate >= monthStart);
        if (scopedToMine) leaveQuery = leaveQuery.Where(item => item.Email == currentUser!.Email);
        var requests = await leaveQuery.OrderBy(item => item.EmployeeName).ToListAsync();

        var summaryQuery = db.LeaveRequests.AsNoTracking().Where(item => item.Status == LeaveStatus.Approved && item.EndDate >= today);
        if (scopedToMine) summaryQuery = summaryQuery.Where(item => item.Email == currentUser!.Email);
        var summaryRequests = await summaryQuery.ToListAsync();
        var todayNames = summaryRequests.Where(item => item.StartDate.Date <= today && item.EndDate.Date >= today).Select(item => item.EmployeeName).Distinct().ToList();
        var daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilNextMonday == 0) daysUntilNextMonday = 7;
        var nextWeekStart = today.AddDays(daysUntilNextMonday);
        var nextWeekEnd = nextWeekStart.AddDays(6);
        var nextWeekPeople = summaryRequests.Where(item => item.StartDate.Date <= nextWeekEnd && item.EndDate.Date >= nextWeekStart).Select(item => item.Email).Distinct().Count();

        var gridStart = monthStart.AddDays(-(((int)monthStart.DayOfWeek + 6) % 7));
        var days = Enumerable.Range(0, 42).Select(offset =>
        {
            var date = gridStart.AddDays(offset);
            var entries = requests
                .Where(item => item.StartDate.Date <= date && item.EndDate.Date >= date)
                .Select(item => new LeaveCalendarEntry(item.EmployeeName, item.LeaveType))
                .ToList();
            return new LeaveCalendarDay
            {
                Date = date,
                IsCurrentMonth = date.Month == selectedMonth,
                IsToday = date == today,
                Entries = entries
            };
        }).ToList();

        return View(new LeaveCalendarViewModel
        {
            MonthTitle = monthStart.ToString("MMMM yyyy"),
            OnlyMine = onlyMine || !User.IsInRole("Manager"),
            TodaySummary = todayNames.Count == 0 ? $"Off today, {today:ddd MMM d}: no approved leave." : $"Off today, {today:ddd MMM d}: {string.Join(", ", todayNames)}.",
            NextWeekPeopleCount = nextWeekPeople,
            PreviousMonth = monthStart.AddMonths(-1),
            NextMonth = monthStart.AddMonths(1),
            Days = days
        });
    }
    [Authorize(Roles = "Manager")]
    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? status = "All")
    {
        var requestedStatus = string.IsNullOrWhiteSpace(status) ? "All" : status.Trim();
        LeaveStatus? selectedStatus = null;
        if (!requestedStatus.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<LeaveStatus>(requestedStatus, true, out var parsedStatus) ||
                parsedStatus is not (LeaveStatus.Pending or LeaveStatus.Approved or LeaveStatus.Rejected or LeaveStatus.NeedsMoreInfo))
                return BadRequest("Choose All, Pending, Approved, Rejected, or NeedsMoreInfo.");
            selectedStatus = parsedStatus;
        }

        var query = db.LeaveRequests.AsNoTracking();
        if (selectedStatus == LeaveStatus.Pending)
            query = query.Where(item => item.Status == LeaveStatus.Pending || item.Status == LeaveStatus.NeedsMoreInfo);
        else if (selectedStatus.HasValue)
            query = query.Where(item => item.Status == selectedStatus.Value);

        var requests = await query.OrderByDescending(item => item.SubmittedAt).ToListAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Request ID,Employee,Email,Leave type,Start date,End date,Days,Status,Submitted at,Decided by,Decision email,Decision time,Rejection reason,More information requested,Employee response,Email notice status,Email sent at");
        foreach (var item in requests)
        {
            var values = new[]
            {
                item.Id.ToString(CultureInfo.InvariantCulture), item.EmployeeName, item.Email, item.LeaveType,
                item.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                item.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                item.Days.ToString(CultureInfo.InvariantCulture), item.Status.ToString(),
                item.SubmittedAt.ToString("O", CultureInfo.InvariantCulture), item.DecisionByName ?? "",
                item.DecisionByEmail ?? "", item.DecisionAt?.ToString("O", CultureInfo.InvariantCulture) ?? "",
                item.RejectionReason ?? "", item.MoreInfoRequest ?? "", item.EmployeeResponse ?? "", item.DecisionEmailStatus, item.DecisionEmailSentAt?.ToString("O", CultureInfo.InvariantCulture) ?? ""
            };
            csv.AppendLine(string.Join(",", values.Select(CsvCell)));
        }

        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        var statusSuffix = selectedStatus?.ToString().ToLowerInvariant() ?? "all";
        return File(content, "text/csv; charset=utf-8", $"dayoff-leave-requests-{statusSuffix}-{DateTime.Today:yyyyMMdd}.csv");
    }

    private static string CsvCell(string? value) => $"\"{(value ?? "").Replace("\"", "\"\"")}\"";
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLeaveRequestViewModel form)
    {
        if (form.StartDate.HasValue && form.EndDate.HasValue && form.EndDate.Value.Date < form.StartDate.Value.Date)
            ModelState.AddModelError(nameof(form.EndDate), "End date must be on or after the start date.");
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault()
                ?? "Please check the details and try again.";
            return ReopenRequestForm(form, message);
        }
        var currentUser = await users.GetUserAsync(User);
        if (currentUser is null) return Challenge();

        if (form.StartDate.HasValue && form.StartDate.Value.Date < DateTime.Today)
            ModelState.AddModelError(nameof(form.StartDate), "Leave cannot start in the past.");
        if (form.StartDate.HasValue && form.EndDate.HasValue)
        {
            var start = form.StartDate.Value.Date;
            var end = form.EndDate.Value.Date;
            if (end < start)
                ModelState.AddModelError(nameof(form.EndDate), "End date must be on or after the start date.");
            else if (end < DateTime.Today)
                ModelState.AddModelError(nameof(form.EndDate), "Leave cannot end in the past.");
            else
            {
                var overlaps = await db.LeaveRequests.AnyAsync(existing =>
                    existing.Email == currentUser.Email &&
                    existing.Status != LeaveStatus.Rejected &&
                    existing.StartDate <= end && existing.EndDate >= start);
                if (overlaps)
                    ModelState.AddModelError(string.Empty, "These dates overlap an existing Pending or Approved request.");
            }
        }

        if (ModelState.IsValid && form.LeaveType == "Annual leave" && form.StartDate.HasValue && form.EndDate.HasValue)
        {
            var allowance = Math.Clamp(currentUser.AnnualLeaveAllowanceDays, 0, 366);
            var start = form.StartDate.Value.Date;
            var end = form.EndDate.Value.Date;
            var existingAnnualRequests = await db.LeaveRequests.AsNoTracking()
                .Where(x => x.Email == currentUser.Email && x.LeaveType == "Annual leave" &&
                            x.Status != LeaveStatus.Rejected)
                .ToListAsync();

            for (var year = start.Year; year <= end.Year; year++)
            {
                var requestedDays = DaysWithinRange(start, end, year);
                var alreadyReserved = existingAnnualRequests.Sum(x => DaysWithinYear(x, year));
                if (alreadyReserved + requestedDays > allowance)
                {
                    ModelState.AddModelError(string.Empty,
                        $"This request would exceed the annual leave allowance of {allowance} days for {year}.");
                    break;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault()
                ?? "Please check the request details and try again.";
            return ReopenRequestForm(form, message);
        }

        var request = new LeaveRequest
        {
            EmployeeName = currentUser.DisplayName,
            Email = currentUser.Email ?? "",
            LeaveType = form.LeaveType,
            StartDate = form.StartDate!.Value.Date,
            EndDate = form.EndDate!.Value.Date,
            Reason = form.Reason,
            Status = LeaveStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync();
        TempData["Success"] = "Leave request submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = await users.GetUserAsync(User);
        if (currentUser is null) return Challenge();

        var request = await db.LeaveRequests.FindAsync(id);
        if (request is null) return NotFound();
        if (!string.Equals(request.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
            return Forbid();
        if (request.Status != LeaveStatus.Pending)
        {
            TempData["ActionError"] = "Only pending leave requests can be deleted. This request has already been decided.";
            return RedirectToAction(nameof(Index));
        }

        db.LeaveRequests.Remove(request);
        await db.SaveChangesAsync();
        TempData["Success"] = "Pending leave request deleted.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(int id, LeaveStatus status)
    {
        if (status is not (LeaveStatus.Approved or LeaveStatus.Rejected)) return BadRequest();
        var rejectionReason = Request.Form["rejectionReason"].ToString().Trim();
        if (status == LeaveStatus.Rejected && (rejectionReason.Length is < 3 or > 500)) { TempData["ActionError"] = "Enter a rejection reason between 3 and 500 characters."; return RedirectToAction(nameof(Index)); }
        var item = await db.LeaveRequests.FindAsync(id);
        if (item is null) return NotFound();
        if (item.Status != LeaveStatus.Pending)
        {
            TempData["ActionError"] = "This request has already been decided.";
            return RedirectToAction(nameof(Index));
        }

        var manager = await users.GetUserAsync(User);
        if (manager is null) return Challenge();
        item.Status = status;
        item.RejectionReason = status == LeaveStatus.Rejected ? rejectionReason : null;
        item.DecisionByName = manager.DisplayName;
        item.DecisionByEmail = manager.Email;
        item.DecisionAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var emailSent = await decisionEmailSender.SendDecisionNoticeAsync(item, manager);
        item.DecisionEmailStatus = !emailOptions.Value.Enabled ? "Disabled" : emailSent ? "Sent" : "Failed";
        item.DecisionEmailSentAt = emailSent ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Request {status.ToString().ToLowerInvariant()}.";
        TempData["UndoRequestId"] = item.Id;
        TempData["UndoDecisionTicks"] = item.DecisionAt.Value.Ticks;
        TempData["UndoExpiresAt"] = DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestMoreInfo(int id, string message)
    {
        message = (message ?? "").Trim();
        if (message.Length is < 3 or > 500) { TempData["ActionError"] = "Enter a message between 3 and 500 characters."; return RedirectToAction(nameof(Index)); }
        var item = await db.LeaveRequests.FindAsync(id);
        if (item is null) return NotFound();
        if (item.Status != LeaveStatus.Pending) { TempData["ActionError"] = "This request is no longer pending."; return RedirectToAction(nameof(Index)); }
        var manager = await users.GetUserAsync(User);
        if (manager is null) return Challenge();
        item.Status = LeaveStatus.NeedsMoreInfo; item.MoreInfoRequest = message; item.EmployeeResponse = null;
        item.DecisionByName = manager.DisplayName; item.DecisionByEmail = manager.Email; item.DecisionAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var emailSent = await decisionEmailSender.SendDecisionNoticeAsync(item, manager);
        item.DecisionEmailStatus = !emailOptions.Value.Enabled ? "Disabled" : emailSent ? "Sent" : "Failed";
        item.DecisionEmailSentAt = emailSent ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Request for more information sent to the employee.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddInformation(int id, string response)
    {
        response = (response ?? "").Trim();
        if (response.Length is < 3 or > 500) { TempData["ActionError"] = "Enter a response between 3 and 500 characters."; return RedirectToAction(nameof(Index)); }
        var currentUser = await users.GetUserAsync(User);
        if (currentUser is null) return Challenge();
        var item = await db.LeaveRequests.FindAsync(id);
        if (item is null) return NotFound();
        if (!string.Equals(item.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase)) return Forbid();
        if (item.Status != LeaveStatus.NeedsMoreInfo) { TempData["ActionError"] = "This request is not waiting for more information."; return RedirectToAction(nameof(Index)); }
        item.EmployeeResponse = response; item.Status = LeaveStatus.Pending; item.SubmittedAt = DateTime.UtcNow;
        item.DecisionByName = null; item.DecisionByEmail = null; item.DecisionAt = null;
        item.DecisionEmailStatus = "Unknown"; item.DecisionEmailSentAt = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Your response was sent. The request is pending review.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UndoDecision(int id, long decisionTicks)
    {
        var item = await db.LeaveRequests.FindAsync(id);
        if (item is null) return NotFound();
        if (item.Status is not (LeaveStatus.Approved or LeaveStatus.Rejected) || item.DecisionAt?.Ticks != decisionTicks || DateTime.UtcNow - item.DecisionAt.Value > TimeSpan.FromSeconds(10))
        { TempData["ActionError"] = "The undo window has expired or this request has changed."; return RedirectToAction(nameof(Index)); }
        item.Status = LeaveStatus.Pending; item.DecisionByName = null; item.DecisionByEmail = null; item.DecisionAt = null;
        item.RejectionReason = null;
        item.DecisionEmailStatus = item.DecisionEmailStatus == "Sent" ? "Sent (cannot recall)" : "Undone";
        item.DecisionEmailSentAt = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Decision undone. The request is pending again; a sent email cannot be recalled.";
        return RedirectToAction(nameof(Index));
    }
    private IActionResult ReopenRequestForm(CreateLeaveRequestViewModel form, string message)
    {
        TempData["RequestError"] = message;
        TempData["RequestModalOpen"] = "true";
        TempData["RequestLeaveType"] = form.LeaveType;
        TempData["RequestStartDate"] = form.StartDate?.ToString("yyyy-MM-dd") ?? "";
        TempData["RequestEndDate"] = form.EndDate?.ToString("yyyy-MM-dd") ?? "";
        TempData["RequestReason"] = form.Reason ?? "";
        return RedirectToAction(nameof(Index));
    }

    private static int DaysWithinYear(LeaveRequest request, int year) =>
        DaysWithinRange(request.StartDate, request.EndDate, year);

    private static int DaysWithinRange(DateTime start, DateTime end, int year)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31);
        var overlapStart = start.Date > yearStart ? start.Date : yearStart;
        var overlapEnd = end.Date < yearEnd ? end.Date : yearEnd;
        return overlapEnd < overlapStart ? 0 : (overlapEnd - overlapStart).Days + 1;
    }
}





