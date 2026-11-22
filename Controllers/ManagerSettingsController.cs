
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TasksFollow.Models;
using TasksFollow.Services;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerSettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailService _emailService;
        private readonly ILogger<ManagerSettingsController> _logger;

        public ManagerSettingsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, EmailService emailService, ILogger<ManagerSettingsController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: ManagerSettings/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Get the currently logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            var model = new ManagerProfileViewModel
            {
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email
            };

            return View(model);
        }

        // POST: ManagerSettings/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ManagerProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;
                return View(model);
            }

            // Update only the manager's personal information
            user.FirstName = model.FirstName.Trim();
            user.MiddleName = model.MiddleName?.Trim();
            user.LastName = model.LastName.Trim();
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                model.Email = user.Email;
                return View(model);
            }

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // GET: ManagerSettings/ChangePassword
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            return View(new ManagerChangePasswordViewModel());
        }

        // POST: ManagerSettings/ChangePassword

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ManagerChangePasswordViewModel model)
        {
            // Get logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            // Validate form
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify current password and update password
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // Refresh authentication session
            await _signInManager.RefreshSignInAsync(user);

            // Send password change confirmation email
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    string subject = "Password Changed Successfully - TasksFollow";

                    string body = "Hello " + user.FirstName + ",\n\n" +
                                  "Your TasksFollow manager account password has been changed successfully.\n\n" +
                                  "Username: " + user.Email + "\n" +
                                  "Date: " + DateTime.Now.ToString("MMM dd, yyyy hh:mm tt") + "\n\n" +
                                  "If you made this change, no further action is required.\n\n" +
                                  "If you did not change your password, please contact your administrator immediately.\n\n" +
                                  "For security reasons, your password is not included in this email.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password change email to manager {ManagerId}", user.Id);
                }
            }

           
            return RedirectToAction(nameof(ChangePassword));
        }

    }
}
