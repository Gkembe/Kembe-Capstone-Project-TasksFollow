using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    
    [Authorize(Roles = "Manager")]
    public class ManagerEmployeeController : Controller
    {
        
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

       
        public ManagerEmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
        // Validate the form, create an Identity user, and save the employee profile.
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
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
            // Prevent multiple accounts from using the same email address.
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // Create Identity account
            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                CompanyID = companyId
            };

            // Create the employee's login account with the supplied password.
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // Assign Employee role
            // Assign the Employee role; delete the account if this step fails.
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
            // Create the employee record linked to the new Identity account.
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

            TempData["SuccessMessage"] = "Employee created successfully.";
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
        // Disable both the employee profile and linked user account.
        public async Task<IActionResult> Deactivate(int id)
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

            // Set both active flags to false so the employee is deactivated.
            employee.IsActive = false;
            employee.ApplicationUser.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee deactivated successfully.";
            return RedirectToAction("Details", new { id });
        }


        // POST: ManagerEmployee/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Reactivate both the employee profile and linked user account.
        public async Task<IActionResult> Activate(int id)
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

            // Set both active flags to true so the employee is reactivated.
            employee.IsActive = true;

            employee.ApplicationUser.IsActive = true;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id });
        }
    }
}
