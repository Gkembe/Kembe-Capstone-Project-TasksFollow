
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;
using TasksFollow.Services;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeSettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<EmployeeSettingsController> _logger;


        public EmployeeSettingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService, ILogger<EmployeeSettingsController> logger, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
            _signInManager = signInManager;
        }

        // GET: EmployeeSettings/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            // Load the logged-in employee's information
            var model = new EmployeeProfileViewModel
            {
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email ?? ""
            };

            return View(model);
        }

        // POST: EmployeeSettings/Profile

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EmployeeProfileViewModel model)
        {
            // Get the currently logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            // Always get the email from the database
            model.Email = user.Email;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update only the employee's personal information
            user.FirstName = model.FirstName.Trim();
            user.MiddleName = model.MiddleName?.Trim();
            user.LastName = model.LastName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // Refresh the authentication session
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction(nameof(Profile));
        }


        // GET: EmployeeSettings/ChangePassword
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            return View(new EmployeeChangePasswordViewModel());
        }

        // POST: EmployeeSettings/ChangePassword

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(EmployeeChangePasswordViewModel model)
        {
            // Get logged-in employee
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

            // Refresh employee authentication session
            await _signInManager.RefreshSignInAsync(user);

            // Send password change confirmation email
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    string subject = "Password Changed Successfully - TasksFollow";

                    string body = "Hello " + user.FirstName + ",\n\n" +
                                  "Your TasksFollow account password has been changed successfully.\n\n" +
                                  "Username: " + user.Email + "\n" +
                                  "Date: " + DateTime.Now.ToString("MMM dd, yyyy hh:mm tt") + "\n\n" +
                                  "If you made this change, no further action is required.\n\n" +
                                  "If you did not change your password, please contact your company manager immediately.\n\n" +
                                  "For security reasons, your password is not included in this email.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password change confirmation to employee {UserId}", user.Id);
                }
            }

            

            return RedirectToAction(nameof(ChangePassword));
        }

    }
}
