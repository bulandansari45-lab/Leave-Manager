using LeaveManagement.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Controllers;

public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> users, IWebHostEnvironment environment) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var account = await users.FindByEmailAsync(model.Email);
        if (account is { IsActive: false })
        {
            ModelState.AddModelError(string.Empty, "This account is inactive. Contact your manager.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return !string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl)
                : RedirectToAction("Index", "Leave");

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "This account is temporarily locked. Try again in five minutes."
            : "Email or password is incorrect.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email);
        if (user is not { IsActive: true })
        {
            ModelState.AddModelError(nameof(model.Email), "No active account was found for that email.");
            return View(model);
        }
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var result = await users.ResetPasswordAsync(user, token, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        TempData["PasswordResetSuccess"] = "Your password has been updated. Sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }
    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(new ProfileViewModel { DisplayName = user.DisplayName, Email = user.Email ?? "", Designation = user.Designation, ProfilePhotoPath = user.ProfilePhotoPath });
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        model.Email = user.Email ?? "";
        model.ProfilePhotoPath = user.ProfilePhotoPath;
        ModelState.Remove(nameof(model.Email));

        if (model.Photo is { Length: > 0 } photo)
        {
            var validExtension = await GetVerifiedPhotoExtensionAsync(photo);
            if (photo.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.Photo), "Choose an image smaller than 5 MB.");
            else if (validExtension is null)
                ModelState.AddModelError(nameof(model.Photo), "Upload a valid JPG, PNG, or WebP image.");
        }
        if (!ModelState.IsValid) return View(model);

        string? newPhotoPath = null;
        if (model.Photo is { Length: > 0 } upload)
        {
            var extension = (await GetVerifiedPhotoExtensionAsync(upload))!;
            var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var directory = Path.Combine(webRoot, "uploads", "profiles");
            Directory.CreateDirectory(directory);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using (var stream = System.IO.File.Create(Path.Combine(directory, fileName)))
                await upload.CopyToAsync(stream);
            newPhotoPath = $"/Account/ProfilePhoto/{fileName}";
        }

        var oldPhotoPath = user.ProfilePhotoPath;
        user.DisplayName = model.DisplayName.Trim();
        user.Designation = (model.Designation ?? "").Trim();
        if (newPhotoPath is not null) user.ProfilePhotoPath = newPhotoPath;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            if (newPhotoPath is not null) DeleteProfilePhoto(newPhotoPath);
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            model.ProfilePhotoPath = oldPhotoPath;
            return View(model);
        }
        if (newPhotoPath is not null) DeleteProfilePhoto(oldPhotoPath);
        await signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Profile));
    }

    private static async Task<string?> GetVerifiedPhotoExtensionAsync(IFormFile file)
    {
        if (file.Length == 0 || file.Length > 5 * 1024 * 1024) return null;
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ".png";
        if (read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255) return ".jpg";
        if (read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }

    private void DeleteProfilePhoto(string? path)
    {
        const string prefix = "/uploads/profiles/";
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(prefix, StringComparison.Ordinal)) return;
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var fileName = Path.GetFileName(path[prefix.Length..]);
        var fullPath = Path.Combine(webRoot, "uploads", "profiles", fileName);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();

        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction("Index", "Leave");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}



