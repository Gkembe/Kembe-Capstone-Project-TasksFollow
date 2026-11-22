using System.ComponentModel.Design;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using TasksFollow.Services;

namespace TasksFollow.Controllers
{
   
    [Authorize(Roles = "Admin")]
    public class ManagerController : Controller
    {
        
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        private readonly EmailService _emailService;
        private readonly ILogger<ManagerController> _logger;


        public ManagerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService, ILogger<ManagerController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }
        // GET: Manager/Index
        // Show all users in the Manager role and their company information.

        [HttpGet]
        public async Task<IActionResult> Index(string search, string status = "Active")
        {
            // Retrieve managers through ASP.NET Core Identity
            var managers = await _userManager.GetUsersInRoleAsync("Manager");

            // Collect manager IDs to load their additional database details
            var managerIds = managers.Select(m => m.Id).ToList();

            // Load manager accounts with their related companies
            var users = await _context.Users.Where(u => managerIds.Contains(u.Id)).Include(u => u.Company).ToListAsync();

            // Search managers by first name, last name, email, or company name
            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Company != null && u.Company.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Filter managers by account status (Active by default)
            if (status == "Active")
            {
                users = users.Where(u => u.IsActive).ToList();
            }
            else if (status == "Inactive")
            {
                users = users.Where(u => !u.IsActive).ToList();
            }

            var managerList = new List<ManagerIndexViewModel>();

            // Convert each user into a ViewModel for the managers list
            foreach (var user in users.OrderBy(u => u.FirstName))
            {
                var manager = new ManagerIndexViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CompanyId = user.CompanyID
                };

                // Display the company name, or a fallback if none is linked
                if (user.Company != null)
                {
                    manager.CompanyName = user.Company.Name;
                }
                else
                {
                    manager.CompanyName = "No Company";
                }

                managerList.Add(manager);
            }

            // Keep the selected search and filter values
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(managerList);
        }





        // GET: Manager/Create
        // Open the form to create a manager for a specific company.
        public async Task<IActionResult> Create(int companyId)
        {
            // Confirm that the selected company exists.
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null)
            {
                return NotFound();
            }

            // Preselect the company when creating the manager.
            var model = new Manager
            {
                CompanyId = company.CompanyId
            };

            // Show the company name on the creation form.
            ViewBag.CompanyName = company.Name;

            return View(model);
        }



        // POST: Manager/Create
        // Validate the form and create a new Identity user in the Manager role.


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Manager model)
        {
            // Check company
            var company = await _context.Companies.FindAsync(model.CompanyId);

            if (company == null)
            {
                return NotFound();
            }

            // Validate form
            if (!ModelState.IsValid)
            {
                ViewBag.CompanyName = company.Name;
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                ViewBag.CompanyName = company.Name;
                return View(model);
            }

            // Create manager account without password
            var manager = new ApplicationUser
            {
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                CompanyID = company.CompanyId,
                EmailConfirmed = false,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(manager);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                ViewBag.CompanyName = company.Name;
                return View(model);
            }

            // Assign Manager role
            var roleResult = await _userManager.AddToRoleAsync(manager, "Manager");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                await _userManager.DeleteAsync(manager);
                ViewBag.CompanyName = company.Name;
                return View(model);
            }

            // Generate password setup token
            string token = await _userManager.GeneratePasswordResetTokenAsync(manager);
            string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Generate password setup link
            string? setupLink = Url.Page("/Account/ResetPassword", pageHandler: null,
                values: new { area = "Identity", code = code, email = manager.Email },
                protocol: Request.Scheme);

            // Send welcome email
            bool emailSent = false;

            if (!string.IsNullOrWhiteSpace(setupLink) && !string.IsNullOrWhiteSpace(manager.Email))
            {
                try
                {
                    string subject = "Welcome to TasksFollow - Manager Account";

                    string body = "Hello " + manager.FirstName + ",\n\n" +
                                  "Your manager account has been created successfully.\n\n" +
                                  "Company: " + company.Name + "\n" +
                                  "Role: Manager\n" +
                                  "Username: " + manager.Email + "\n\n" +
                                  "Please click the link below to create your password:\n" +
                                  setupLink + "\n\n" +
                                  "After setting your password, you can log in to TasksFollow and manage your company's projects, tasks, and employees.\n\n" +
                                  "If you did not expect this invitation, please contact your administrator.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(manager.Email, subject, body);
                    emailSent = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send manager invitation to user {UserId}", manager.Id);
                }
            }

            //if (emailSent)
            //{
            //    TempData["SuccessMessage"] = "Manager created successfully. Invitation email submitted.";
            //}
            //else
            //{
            //    TempData["ErrorMessage"] = "Manager created, but the invitation email could not be sent.";
            //}

            return RedirectToAction("Details", "Company", new { id = company.CompanyId });
        }





        // GET: Manager/Edit/{id}
        // Load an existing manager and the companies available for selection.
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            // Require a manager ID.
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Do not allow this form to edit users outside the Manager role.
            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            // Copy the manager's existing information into the edit form.
            var manager = new ManagerEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CompanyId = user.CompanyID
            };

            // Populate the company dropdown with active companies.
            var companies = await _context.Companies.Where(c => c.IsActive).ToListAsync();

            foreach (var company in companies)
            {
                manager.Companies.Add(new SelectListItem
                {
                    Value = company.CompanyId.ToString(),
                    Text = company.Name
                });
            }

            return View(manager);
        }



        // POST: Manager/Edit
        // Save changes to an existing manager's profile and company association.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ManagerEditViewModel manager)
        {
            // Rebuild the company dropdown if validation fails.
            if (!ModelState.IsValid)
            {
                var companies = await _context.Companies.Where(c => c.IsActive).ToListAsync();

                foreach (var company in companies)
                {
                    manager.Companies.Add(new SelectListItem
                    {
                        Value = company.CompanyId.ToString(),
                        Text = company.Name
                    });
                }

                return View(manager);
            }

            // Find the Identity account being edited.
            var user = await _userManager.FindByIdAsync(manager.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Apply the submitted profile information to the account.
            user.FirstName = manager.FirstName;
            user.MiddleName = manager.MiddleName;
            user.LastName = manager.LastName;
            user.Email = manager.Email;
            user.UserName = manager.Email;
            user.PhoneNumber = manager.PhoneNumber;
            user.CompanyID = manager.CompanyId;

            // Save the updated Identity user.
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                // Display errors if Identity rejects the update.
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(manager);
            }

            return RedirectToAction("Details", new { id = user.Id });
        }



        // GET: Manager/Details/{id}
        // Show a manager's profile and associated company details.
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // Find the user and load company information.
            var user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Verify that this account actually belongs to the Manager role.
            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            // Prepare the manager's information for the details page.
            var manager = new ManagerDetailsViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                CompanyId = user.CompanyID
            };

            // Add company name and status when a company is linked.
            if (user.Company != null)
            {
                manager.CompanyName = user.Company.Name;
                manager.CompanyIsActive = user.Company.IsActive;
            }
            else
            {
                manager.CompanyName = "No Company";
                manager.CompanyIsActive = false;
            }

            return View(manager);
        }



        // POST: Manager/Deactivate
        // Disable a manager account without deleting it from the database.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            // Find manager
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var manager = await _userManager.FindByIdAsync(id);

            if (manager == null || !await _userManager.IsInRoleAsync(manager, "Manager"))
            {
                return NotFound();
            }

            // Prevent duplicate deactivation
            if (!manager.IsActive)
            {
              
                return RedirectToAction(nameof(Index));
            }

            // Deactivate manager and block login
            manager.IsActive = false;
            manager.LockoutEnabled = true;
            manager.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(manager);

            if (!result.Succeeded)
            {
                
                return RedirectToAction(nameof(Index));
            }

            // Invalidate existing login sessions
            var stampResult = await _userManager.UpdateSecurityStampAsync(manager);

            if (!stampResult.Succeeded)
            {
                _logger.LogWarning("Unable to invalidate sessions for manager {ManagerId}", manager.Id);
            }

            // Send email notification
            if (!string.IsNullOrWhiteSpace(manager.Email))
            {
                try
                {
                    string subject = "Account Deactivated - TasksFollow";

                    string body = "Hello " + manager.FirstName + ",\n\n" +
                                  "Your TasksFollow manager account has been deactivated by an administrator.\n\n" +
                                  "Username: " + manager.Email + "\n" +
                                  "Account Status: Deactivated\n\n" +
                                  "You can no longer log in to your TasksFollow account.\n\n" +
                                  "If you believe this is a mistake, please contact your administrator.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(manager.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send deactivation email to manager {ManagerId}", manager.Id);
                }
            }

           
            return RedirectToAction(nameof(Index));
        }


        // Reactivate manager account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            // Find manager
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var manager = await _userManager.FindByIdAsync(id);

            if (manager == null || !await _userManager.IsInRoleAsync(manager, "Manager"))
            {
                return NotFound();
            }

          

            // Reactivate manager and allow login
            manager.IsActive = true;
            manager.LockoutEnabled = true;
            manager.LockoutEnd = null;
            manager.AccessFailedCount = 0;

            var result = await _userManager.UpdateAsync(manager);

            if (!result.Succeeded)
            {
                
                return RedirectToAction(nameof(Index));
            }

            // Send email notification
            if (!string.IsNullOrWhiteSpace(manager.Email))
            {
                try
                {
                    string subject = "Account Reactivated - TasksFollow";

                    string body = "Hello " + manager.FirstName + ",\n\n" +
                                  "Your TasksFollow manager account has been reactivated by an administrator.\n\n" +
                                  "Username: " + manager.Email + "\n" +
                                  "Account Status: Active\n\n" +
                                  "You can now log in to TasksFollow using your existing credentials.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(manager.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send activation email to manager {ManagerId}", manager.Id);
                }
            }

            
            return RedirectToAction(nameof(Index));
        }


        // GET: Manager/ResetPassword/id
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            // Find the manager account
            var manager = await _userManager.FindByIdAsync(id);

            if (manager == null || !await _userManager.IsInRoleAsync(manager, "Manager"))
            {
                return NotFound();
            }

            var model = new AdminResetPasswordViewModel
            {
                ManagerId = manager.Id,
                ManagerName = manager.FirstName + " " + manager.LastName
            };

            return View(model);
        }

        // POST: Manager/ResetPassword

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
        {
            // Find the manager account
            var manager = await _userManager.FindByIdAsync(model.ManagerId);

            if (manager == null || !await _userManager.IsInRoleAsync(manager, "Manager"))
            {
                return NotFound();
            }

            // Generate password reset token
            string token = await _userManager.GeneratePasswordResetTokenAsync(manager);
            string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Generate secure reset link
            string? resetLink = Url.Page("/Account/ResetPassword", pageHandler: null,
                values: new { area = "Identity", code = code, email = manager.Email },
                protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(manager.Email) || string.IsNullOrWhiteSpace(resetLink))
            {
               
                return RedirectToAction("Details", new { id = manager.Id });
            }

            // Send password reset email
            try
            {
                string subject = "Reset Your Password - TasksFollow";

                string body = "Hello " + manager.FirstName + ",\n\n" +
                              "An administrator has requested a password reset for your TasksFollow manager account.\n\n" +
                              "Username: " + manager.Email + "\n\n" +
                              "Please click the secure link below to create a new password:\n" +
                              resetLink + "\n\n" +
                              "If you did not expect this request, please contact your administrator.\n\n" +
                              "TasksFollow Team";

                await _emailService.SendEmailAsync(manager.Email, subject, body);

             
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to manager {ManagerId}", manager.Id);
                TempData["ErrorMessage"] = "Unable to send password reset email. Please try again.";
            }

            return RedirectToAction("Details", new { id = manager.Id });
        }



    }

}
