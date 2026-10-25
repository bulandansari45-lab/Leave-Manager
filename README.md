# DayOff — Leave Management

An internship portfolio project built with ASP.NET Core MVC, C#, Razor, HTML, CSS, and JavaScript.

## Run locally

1. Install the .NET 9 SDK.
2. Open a terminal in this folder.
3. Run `dotnet run` and open the local URL shown in the terminal.

The app connects to SQL Server LocalDB using Entity Framework Core. EF Core migrations create or update `DayOffLeaveDb` for leave requests and `DayOffIdentityDb` for accounts and roles. Existing account and leave data is retained.

## Current features

- Leave overview with request counts and approved days
- New request form with required fields and date checks
- Manager decisions use a featured pending request, large actions, reasoned rejections, request-more-information, a 10-second undo window, and status-filtered CSV export
- Searchable, date-sorted request tabs with a compact summary strip, responsive table, and dark mode
- Shared monthly leave calendar displays approved absences without request reasons
- Employee and Manager sign-in with role-based server authorization
- Employees see only their own requests and can delete them while Pending; managers see the team queue
- Approved and rejected requests show the Manager and time of the decision, plus Gmail notification delivery status
- Optional Gmail notices include rejection reasons and manager questions requesting more information
- Managers can create employee accounts, review per-employee annual leave balances, set allowances, deactivate/reactivate accounts, and review recent account changes; employees can change their password and respond to information requests
- Requests must use current or future dates and cannot overlap the same employee's Pending or Approved request
- Employees can see annual leave allowance, approved usage, pending reservations, and remaining days
- Annual leave requests are limited by the configurable `LeavePolicy:AnnualAllowanceDays` setting (default: 20 calendar days per year)

## Demo sign-ins

- Employee: `aarav@example.com` / `Employee123!`
- Manager: `manager@dayoff.local` / `Manager123!`

These fixed passwords are for local Development only. The app requires `SeedAccounts:EmployeePassword` and `SeedAccounts:ManagerPassword` when running outside Development. Set them with secure environment variables such as `SeedAccounts__EmployeePassword` and `SeedAccounts__ManagerPassword`; do not commit passwords to source control. Seed passwords are used only when a seeded user is first created and do not change passwords for existing accounts. Change existing demo account passwords through the app before sharing it.

Gmail decision emails use Gmail SMTP with STARTTLS on port 587 and are disabled by default. Turn on 2-Step Verification for the sending Google account and create an App Password. Google Workspace or school accounts may restrict App Passwords; ask the administrator if the option is unavailable. Set `Email:GmailAddress` in `appsettings.json`, then store `Email:AppPassword` with Visual Studio's Manage User Secrets or `dotnet user-secrets set "Email:AppPassword" "<app-password>"` (or environment variable `Email__AppPassword`). Never store the App Password in source-controlled settings. Keep `Email:Enabled` false until the settings are ready; then set it to true to send a notice after manager decisions. Google requires 2-Step Verification for App Passwords and may revoke them when the Google account password changes.

Set `LeavePolicy:AnnualAllowanceDays` in `appsettings.json` to choose the default allowance for new accounts. Managers can change each employee's allowance on the Team page. Approved and pending annual leave requests count against the employee's allowance; rejected requests do not. Days are counted inclusively as calendar days. EF Core migrations update the Identity and leave databases without deleting existing account or request data.

## Project status

Identity and leave migrations are applied on app startup. The latest leave migration adds rejection explanations and request/response notes; existing database records are preserved.
