
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerSettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ManagerSettingsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Identity verifies the current password before updating it
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // Refresh the authentication cookie after the password change
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(ChangePassword));
        }
    }
}
