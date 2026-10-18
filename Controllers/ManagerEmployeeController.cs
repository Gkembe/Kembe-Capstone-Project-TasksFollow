
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



        public async Task<IActionResult> Index(string? search, string? status, string? department)
        {
            var user = await _userManager.GetUserAsync(User);
            {
                if (user == null || user.CompanyID == null)
                {

                    return Forbid();

                }

                int companyId = user.CompanyID.Value;

                // Default to Active only when no status is supplied
                if (!Request.Query.ContainsKey("status"))
                {
                    status = "Active";
                }

                var employees = _context.Employees
                    .Include(e => e.ApplicationUser)
                    .Where(e => e.CompanyId == companyId)
                    .AsQueryable();

                // Search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    employees = employees.Where(e => e.ApplicationUser.FirstName.Contains(search) ||
                        e.ApplicationUser.LastName.Contains(search) ||
                        (e.ApplicationUser.Email != null && e.ApplicationUser.Email.Contains(search)) ||
                        e.JobTitle.Contains(search) || (e.Department != null && e.Department.Contains(search)));
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

                var departments = await _context.Employees
                    .Where(e => e.CompanyId == companyId && e.Department != null && e.Department != "")
                    .Select(e => e.Department!)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                ViewBag.Search = search;

                ViewBag.Status = status;

                ViewBag.Department = department;
                ViewBag.Departments = departments;

                return View(await employees.OrderBy(e => e.ApplicationUser.FirstName).ToListAsync());
            }
        }



        // GET: ManagerEmployee/Create
        [HttpGet]
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

            TempData["SuccessMessage"] = "Employee created successfully.";

            return RedirectToAction(nameof(Index));
        }



        // GET: ManagerEmployee/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var employee = await _context.Employees
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

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

            var employee = await _context.Employees
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId && e.CompanyId == manager.CompanyID.Value);

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
        public async Task<IActionResult> Details(int id)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            var employee = await _context.Employees
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

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
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var employee = await _context.Employees
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {

                return NotFound();
            }

            employee.IsActive = false;
            employee.ApplicationUser.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee deactivated successfully.";

            return RedirectToAction("Details", new { id });
        }

        // POST: ManagerEmployee/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var employee = await _context.Employees
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == id && e.CompanyId == manager.CompanyID.Value);

            if (employee == null)
            {

                return NotFound();

            }

            employee.IsActive = true;
            employee.ApplicationUser.IsActive = true;
            await _context.SaveChangesAsync();


            return RedirectToAction("Details", new { id });
        }



    }
}
