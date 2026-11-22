using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;
using TasksFollow.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using TasksFollow.Services;

namespace TasksFollow.Controllers
{
    
    [Authorize(Roles = "Manager")]
    public class ManagerEmployeeController : Controller
    {
        
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly EmailService _emailService;
        private readonly ILogger<ManagerEmployeeController> _logger;

        public ManagerEmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService, ILogger<ManagerEmployeeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: ManagerEmployee/Index
        // List company employees with optional search, status, and department filters.
        public async Task<IActionResult> Index(string? search, string? status, string? department)
        {
            // Get the currently signed-in manager.
            var user = await _userManager.GetUserAsync(User);
            {
                if (user == null || user.CompanyID == null)
                {
                    return Forbid();
                }

                // Limit employee data to the manager's company.
                int companyId = user.CompanyID.Value;

                // Default to Active only when no status is supplied
                if (!Request.Query.ContainsKey("status"))
                {
                    status = "Active";
                }

                // Start the employee query and load associated user information.
                var employees = _context.Employees.Include(e => e.ApplicationUser).Where(e => e.CompanyId == companyId).AsQueryable();

                // Search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    employees = employees.Where(e => e.ApplicationUser.FirstName.Contains(search) ||
                        e.ApplicationUser.LastName.Contains(search) || (e.ApplicationUser.Email != null && e.ApplicationUser.Email.Contains(search)) || e.JobTitle.Contains(search) || (e.Department != null && e.Department.Contains(search)));
                }

                // Status filter
                if (status == "Active")
                {
                    employees = employees.Where(e => e.IsActive);
                }
                else if (status == "Inactive")
                {
                    employees = employees.Where(e => !e.IsActive);
                }

                // Department filter
                if (!string.IsNullOrWhiteSpace(department))
                {
                    employees = employees.Where(e => e.Department == department);
                }

                // Populate the department filter with distinct company departments.
                var departments = await _context.Employees.Where(e => e.CompanyId == companyId && e.Department != null && e.Department != "").Select(e => e.Department!).Distinct().OrderBy(d => d).ToListAsync();

                // Preserve the selected filters when the page is displayed again.
                ViewBag.Search = search;

                ViewBag.Status = status;

                ViewBag.Department = department;
                ViewBag.Departments = departments;
                return View(await employees.OrderBy(e => e.ApplicationUser.FirstName).ToListAsync());
            }
        }

        // GET: ManagerEmployee/Create
        [HttpGet]
        // Display an empty employee registration form.
        public IActionResult Create()
        {
            return View(new CreateEmployeeViewModel());
        }



        // POST: ManagerEmployee/Create


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            int companyId = manager.CompanyID.Value;

            // Check company
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null || !company.IsActive)
            {
                return Forbid();
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // Create Identity account without a password
            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                CompanyID = companyId
            };

            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // Assign Employee role
            var roleResult = await _userManager.AddToRoleAsync(user, "Employee");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                await _userManager.DeleteAsync(user);
                return View(model);
            }

            // Create employee profile
            var employee = new Employee
            {
                ApplicationUserId = user.Id,
                CompanyId = companyId,
                JobTitle = model.JobTitle,
                Department = model.Department,
                AddedDate = DateTime.Now,
                IsActive = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Generate password setup token
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);
            string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Generate password setup link
            string? setupLink = Url.Page("/Account/ResetPassword", pageHandler: null,
                values: new { area = "Identity", code = code, email = user.Email },
                protocol: Request.Scheme);

            // Send welcome email
            bool emailSent = false;

            if (!string.IsNullOrWhiteSpace(setupLink))
            {
                try
                {
                    string subject = "Welcome to TasksFollow - Set Up Your Account";

                    string body = "Hello " + user.FirstName + ",\n\n" +
                                  "Your employee account has been created successfully.\n\n" +
                                  "Company: " + company.Name + "\n" +
                                  "Username: " + user.Email + "\n\n" +
                                  "Please click the link below to create your password:\n" +
                                  setupLink + "\n\n" +
                                  "After setting your password, you can log in to TasksFollow.\n\n" +
                                  "If you did not expect this invitation, contact your company manager.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(user.Email!, subject, body);

                    emailSent = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send employee invitation to user {UserId}", user.Id);
                }
            }

            if (emailSent)
            {
                TempData["SuccessMessage"] = "Employee created successfully. Invitation email submitted.";
            }
            else
            {
                TempData["SuccessMessage"] = "Employee created successfully, but the invitation email could not be sent.";
            }

            return RedirectToAction(nameof(Index));
        }






        // GET: ManagerEmployee/Edit/5
        [HttpGet]
        // Load the company employee and populate the edit form.
        public async Task<IActionResult> Edit(int id)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            var employee = await _context.Employees.Include(e => e.ApplicationUser).FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {
                return NotFound();
            }

            var model = new EditEmployeeViewModel
            {
                EmployeeId = employee.EmployeeId,
                FirstName = employee.ApplicationUser.FirstName,
                LastName = employee.ApplicationUser.LastName,
                MiddleName = employee.ApplicationUser.MiddleName,
                JobTitle = employee.JobTitle,
                Department = employee.Department
            };
            return View(model);
        }



        // POST: ManagerEmployee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Validate and save changes to employee and user profile fields.
        public async Task<IActionResult> Edit(EditEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            var employee = await _context.Employees.Include(e => e.ApplicationUser).FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {
                return NotFound();
            }

            employee.ApplicationUser.FirstName = model.FirstName;
            employee.ApplicationUser.LastName = model.LastName;
            employee.ApplicationUser.MiddleName = model.MiddleName;
            employee.JobTitle = model.JobTitle;
            employee.Department = model.Department;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee updated successfully.";

            return RedirectToAction("Details", new { id = employee.EmployeeId });
        }



        // GET: ManagerEmployee/Details/5
        // Display information for an employee from the manager's company.
        public async Task<IActionResult> Details(int id)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            var employee = await _context.Employees.Include(e => e.ApplicationUser).FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }



        // POST: ManagerEmployee/Deactivate/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            // Get logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find employee in manager's company
            var employee = await _context.Employees.Include(e => e.ApplicationUser).FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {
                return NotFound();
            }

            // Prevent duplicate deactivation
            if (!employee.IsActive && !employee.ApplicationUser.IsActive)
            {
               
                return RedirectToAction("Details", new { id });
            }

            // Deactivate employee and block login
            employee.IsActive = false;
            employee.ApplicationUser.IsActive = false;
            employee.ApplicationUser.LockoutEnabled = true;
            employee.ApplicationUser.LockoutEnd = DateTimeOffset.MaxValue;

            // Save changes
            await _context.SaveChangesAsync();

            // Invalidate existing sessions
            var stampResult = await _userManager.UpdateSecurityStampAsync(employee.ApplicationUser);

            if (!stampResult.Succeeded)
            {
                _logger.LogWarning("Unable to invalidate sessions for employee {EmployeeId}", id);
            }

            // Send email notification
            if (!string.IsNullOrWhiteSpace(employee.ApplicationUser.Email))
            {
                try
                {
                    string subject = "Account Deactivated - TasksFollow";

                    string body = "Hello " + employee.ApplicationUser.FirstName + ",\n\n" +
                                  "Your TasksFollow employee account has been deactivated by your company manager.\n\n" +
                                  "Username: " + employee.ApplicationUser.Email + "\n" +
                                  "Account Status: Deactivated\n\n" +
                                  "You can no longer log in to TasksFollow.\n\n" +
                                  "If you believe this is a mistake, please contact your company manager.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(employee.ApplicationUser.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send deactivation email to employee {EmployeeId}", id);
                }
            }

          
            return RedirectToAction("Details", new { id });
        }



        // POST: ManagerEmployee/Activate/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            // Get logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find employee in manager's company
            var employee = await _context.Employees.Include(e => e.ApplicationUser).FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {
                return NotFound();
            }

            // Prevent duplicate activation
            if (employee.IsActive && employee.ApplicationUser.IsActive)
            {
               
                return RedirectToAction("Details", new { id });
            }

            // Activate employee and allow login
            employee.IsActive = true;
            employee.ApplicationUser.IsActive = true;
            employee.ApplicationUser.LockoutEnabled = true;
            employee.ApplicationUser.LockoutEnd = null;
            employee.ApplicationUser.AccessFailedCount = 0;

            // Save changes
            await _context.SaveChangesAsync();

            // Send email notification
            if (!string.IsNullOrWhiteSpace(employee.ApplicationUser.Email))
            {
                try
                {
                    string subject = "Account Reactivated - TasksFollow";

                    string body = "Hello " + employee.ApplicationUser.FirstName + ",\n\n" +
                                  "Your TasksFollow employee account has been reactivated by your company manager.\n\n" +
                                  "Username: " + employee.ApplicationUser.Email + "\n" +
                                  "Account Status: Active\n\n" +
                                  "You can now log in to TasksFollow using your existing credentials.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(employee.ApplicationUser.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send activation email to employee {EmployeeId}", id);
                }
            }

            return RedirectToAction("Details", new { id });
        }



        [HttpGet]
        public async Task<IActionResult> ResetPassword(int id)
        {
            // Get logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find employee in manager's company
            var employee = await _context.Employees.Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordConfirmed(int id)
        {
            // Get logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Check company
            bool companyActive = await _context.Companies
                .AnyAsync(c => c.CompanyId == manager.CompanyID.Value && c.IsActive);

            if (!companyActive)
            {
                return Forbid();
            }

            // Find employee belonging to manager's company
            var employee = await _context.Employees.Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {
                return NotFound();
            }

            var user = employee.ApplicationUser;

            if (string.IsNullOrWhiteSpace(user.Email))
            {
               
                return RedirectToAction("Details", new { id = id });
            }

            // Generate password reset token
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);
            string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Generate secure password reset link
            string? resetLink = Url.Page("/Account/ResetPassword", pageHandler: null,
                values: new { area = "Identity", code = code, email = user.Email },
                protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(resetLink))
            {
               
                return RedirectToAction("Details", new { id = id });
            }

            // Send password reset email
            try
            {
                string subject = "Reset Your Password - TasksFollow";

                string body = "Hello " + user.FirstName + ",\n\n" +
                              "Your company manager has requested a password reset for your TasksFollow account.\n\n" +
                              "Username: " + user.Email + "\n\n" +
                              "Please use the secure link below to create a new password:\n" +
                              resetLink + "\n\n" +
                              "If you did not expect this request, please contact your company manager.\n\n" +
                              "TasksFollow Team";

                await _emailService.SendEmailAsync(user.Email, subject, body);

                TempData["SuccessMessage"] = "Password reset email submitted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to employee {EmployeeId}", id);
               
            }

            return RedirectToAction("Details", new { id = id });
        }


    }
}
