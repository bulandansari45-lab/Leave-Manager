using LeaveManagement.Models;
using LeaveManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeaveManagement.Controllers;

[Authorize(Roles = "Manager")]
public class TeamController(UserManager<ApplicationUser> users, AuthDbContext authDb, LeaveDbContext leaveDb, IOptions<LeavePolicyOptions> leavePolicy) : Controller
{
    public async Task<IActionResult> Index()
    {
        var employees = await users.GetUsersInRoleAsync("Employee");
        var year = DateTime.Today.Year;
        var rows = await leaveDb.LeaveRequests.AsNoTracking().Where(x => x.LeaveType == "Annual leave" && x.StartDate <= new DateTime(year, 12, 31) && x.EndDate >= new DateTime(year, 1, 1) && x.Status != LeaveStatus.Rejected).ToListAsync();
        ViewBag.LeaveRows = rows;
        ViewBag.BalanceYear = year;
        return View(employees.OrderBy(x => x.DisplayName).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAllowance(string userId, int annualAllowanceDays)
    {
        if (annualAllowanceDays is < 0 or > 366)
        {
            TempData["ActionError"] = "Annual allowance must be between 0 and 366 days.";
            return RedirectToAction(nameof(Index));
        }

        var employee = await users.FindByIdAsync(userId);
        if (employee is null || !await users.IsInRoleAsync(employee, "Employee")) return NotFound();

        employee.AnnualLeaveAllowanceDays = annualAllowanceDays;
        var result = await users.UpdateAsync(employee);
        if (!result.Succeeded)
        {
            TempData["ActionError"] = string.Join(" ", result.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Annual leave allowance updated for {employee.DisplayName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string userId, bool isActive)
    {
        var employee = await users.FindByIdAsync(userId);
        if (employee is null || !await users.IsInRoleAsync(employee, "Employee")) return NotFound();

        if (employee.IsActive == isActive)
        {
            TempData["Success"] = $"{employee.DisplayName}'s account is already {(isActive ? "active" : "inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        employee.IsActive = isActive;
        var result = await users.UpdateAsync(employee);
        if (!result.Succeeded)
        {
            TempData["ActionError"] = string.Join(" ", result.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Index));
        }

        var manager = await users.GetUserAsync(User);
        authDb.AccountStatusChanges.Add(new AccountStatusChange
        {
            EmployeeUserId = employee.Id,
            EmployeeName = employee.DisplayName,
            EmployeeEmail = employee.Email ?? string.Empty,
            ChangedByName = manager?.DisplayName ?? User.Identity?.Name ?? "Manager",
            ChangedByEmail = manager?.Email ?? "unknown",
            IsActive = isActive,
            ChangedAtUtc = DateTime.UtcNow
        });
        await authDb.SaveChangesAsync();

        TempData["Success"] = isActive
            ? $"{employee.DisplayName}'s account has been reactivated."
            : $"{employee.DisplayName}'s account has been deactivated. Their leave history is retained.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateEmployeeViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEmployeeViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var employee = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            DisplayName = model.DisplayName.Trim(),
            EmailConfirmed = true,
            AnnualLeaveAllowanceDays = Math.Clamp(leavePolicy.Value.AnnualAllowanceDays, 0, 366),
            IsActive = true
        };

        var result = await users.CreateAsync(employee, model.InitialPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var roleResult = await users.AddToRoleAsync(employee, "Employee");
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(employee);
            foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Success"] = $"Employee account created for {employee.DisplayName}. Share the initial password with them securely.";
        return RedirectToAction(nameof(Index));
    }
}
