using LeaveManagement.Data;
using LeaveManagement.Models;
using LeaveManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.Configure<LeavePolicyOptions>(builder.Configuration.GetSection("LeavePolicy"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IDecisionEmailSender, SmtpDecisionEmailSender>();
builder.Services.AddDbContext<LeaveDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LeaveDatabase")));
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDatabase")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    var validatePrincipal = options.Events.OnValidatePrincipal;
    options.Events.OnValidatePrincipal = async context =>
    {
        if (validatePrincipal is not null)
            await validatePrincipal(context);

        if (context.Principal?.Identity?.IsAuthenticated != true)
            return;

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal);
        if (user is null || !user.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

var app = builder.Build();
var defaultAnnualAllowanceDays = Math.Clamp(builder.Configuration.GetValue<int>("LeavePolicy:AnnualAllowanceDays", 20), 0, 366);
var employeeSeedPassword = builder.Configuration["SeedAccounts:EmployeePassword"];
var managerSeedPassword = builder.Configuration["SeedAccounts:ManagerPassword"];
if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(employeeSeedPassword) || string.IsNullOrWhiteSpace(managerSeedPassword)))
    throw new InvalidOperationException("Set SeedAccounts:EmployeePassword and SeedAccounts:ManagerPassword before running outside Development.");

await SeedIdentityAsync(
    app.Services,
    defaultAnnualAllowanceDays,
    employeeSeedPassword ?? "Employee123!",
    managerSeedPassword ?? "Manager123!");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LeaveDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("""
        IF COL_LENGTH(N'dbo.LeaveRequests', N'DecisionByName') IS NULL
            ALTER TABLE [dbo].[LeaveRequests] ADD [DecisionByName] nvarchar(100) NULL;
        IF COL_LENGTH(N'dbo.LeaveRequests', N'DecisionByEmail') IS NULL
            ALTER TABLE [dbo].[LeaveRequests] ADD [DecisionByEmail] nvarchar(160) NULL;
        IF COL_LENGTH(N'dbo.LeaveRequests', N'DecisionAt') IS NULL
            ALTER TABLE [dbo].[LeaveRequests] ADD [DecisionAt] datetime2 NULL;
        """);
    if (!db.LeaveRequests.Any())
    {
        db.LeaveRequests.AddRange(
            new LeaveRequest { EmployeeName = "Aarav Sharma", Email = "aarav@example.com", LeaveType = "Annual leave", StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), Reason = "Family trip", Status = LeaveStatus.Pending },
            new LeaveRequest { EmployeeName = "Maya Patel", Email = "maya@example.com", LeaveType = "Sick leave", StartDate = DateTime.Today.AddDays(-8), EndDate = DateTime.Today.AddDays(-7), Reason = "Medical rest", Status = LeaveStatus.Approved },
            new LeaveRequest { EmployeeName = "Rohan Mehta", Email = "rohan@example.com", LeaveType = "Personal leave", StartDate = DateTime.Today.AddDays(12), EndDate = DateTime.Today.AddDays(12), Reason = "Personal appointment", Status = LeaveStatus.Rejected });
        db.SaveChanges();
    }
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Leave}/{action=Index}/{id?}").WithStaticAssets();
app.Run();

static async Task SeedIdentityAsync(IServiceProvider services, int defaultAnnualAllowanceDays, string employeeSeedPassword, string managerSeedPassword)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();


    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Employee", "Manager" })
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new IdentityRole(role));

    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await EnsureUserAsync(users, "aarav@example.com", "Aarav Sharma", employeeSeedPassword, "Employee", defaultAnnualAllowanceDays);
    await EnsureUserAsync(users, "manager@dayoff.local", "Morgan Manager", managerSeedPassword, "Manager", defaultAnnualAllowanceDays);
}

static async Task EnsureUserAsync(UserManager<ApplicationUser> users, string email, string displayName, string password, string role, int defaultAnnualAllowanceDays)
{
    var user = await users.FindByEmailAsync(email);
    if (user is null)
    {
        user = new ApplicationUser { UserName = email, Email = email, DisplayName = displayName, EmailConfirmed = true, AnnualLeaveAllowanceDays = defaultAnnualAllowanceDays };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not seed {role} account: {string.Join("; ", result.Errors.Select(x => x.Description))}");
    }

    if (!await users.IsInRoleAsync(user, role))
        await users.AddToRoleAsync(user, role);
}


