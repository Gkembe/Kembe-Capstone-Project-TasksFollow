
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManagerProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }

        // GET: ManagerProject/Index

        public async Task<IActionResult> Index(string? search, string? status, DateTime? startDate, DateTime? dueDate)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            int companyId = manager.CompanyID.Value;

            var projects = _context.Projects
                .Include(p => p.CreatedByUser)
                .Where(p => p.CompanyId == companyId)
                .AsQueryable();

            // Search by name or description
            if (!string.IsNullOrWhiteSpace(search))
            {
                projects = projects.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                projects = projects.Where(p => p.Status == status);
            }

            // Filter by start date
            if (startDate.HasValue)
            {
                projects = projects.Where(p => p.StartDate.HasValue && p.StartDate.Value.Date >= startDate.Value.Date);
            }

            // Filter by due date
            if (dueDate.HasValue)
            {
                projects = projects.Where(p => p.DueDate.HasValue && p.DueDate.Value.Date <= dueDate.Value.Date);
            }

            var result = await projects.OrderByDescending(p => p.ProjectId).ToListAsync();

            ViewBag.Search = search;

            ViewBag.Status = status;

            ViewBag.StartDate = startDate;

            ViewBag.DueDate = dueDate;

            return View(result);
        }





        // GET: ManagerProject/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateProjectViewModel());
        }

        // POST: ManagerProject/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectViewModel model)
        {
            // Validate dates
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.DueDate.Value.Date < model.StartDate.Value.Date)
            {

                ModelState.AddModelError("DueDate", "Due date cannot be earlier than start date.");

            }

            if (!ModelState.IsValid)
            {

                return View(model);

            }

            // Get logged-in manager
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null) return Forbid();

            // Check company
            var company = await _context.Companies.FindAsync(manager.CompanyID.Value);


            if (company == null || !company.IsActive)
            {

                return Forbid();
            }

            // Validate status
            if (model.Status != "Not Started" && model.Status != "In Progress" && model.Status != "Completed")
            {
                ModelState.AddModelError("Status", "Please select a valid project status.");

                return View(model);
            }

            // Create project
            var project = new Project
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                DueDate = model.DueDate,
                Status = model.Status,
                CompanyId = company.CompanyId,
                CreatedByUserId = manager.Id
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

           
            return RedirectToAction("Index");
        }




        // GET: ManagerProject/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {

                return NotFound();
            }

            var model = new EditProjectViewModel
            {
                ProjectId = project.ProjectId,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                DueDate = project.DueDate,
                Status = project.Status
            };

            return View(model);
        }

        // POST: ManagerProject/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProjectViewModel model)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();
            }

            if (model.StartDate.HasValue && model.DueDate.HasValue && model.DueDate.Value.Date < model.StartDate.Value.Date)
            {

                ModelState.AddModelError("DueDate", "Due date cannot be earlier than start date.");

            }

            if (model.Status != "Not Started" && model.Status != "In Progress" && model.Status != "Completed")
            {

                ModelState.AddModelError("Status", "Please select a valid status.");
            }

            if (!ModelState.IsValid)
            {

                return View(model);
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {

                return NotFound();

            }

            project.Name = model.Name;
            project.Description = model.Description;
            project.StartDate = model.StartDate;
            project.DueDate = model.DueDate;
            project.Status = model.Status;

            await _context.SaveChangesAsync();

           
            return RedirectToAction("Details", new { id = project.ProjectId });
        }



        // GET: ManagerProject/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var project = await _context.Projects
                .Include(p => p.CreatedByUser)
                .Include(p => p.ProjectMembers)
                .Include(p => p.ProjectTask)
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {

                return NotFound();

            }

            return View(project);
        }




        [HttpGet]
        public async Task<IActionResult> Members(int id)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.Employee)
                        .ThenInclude(e => e.ApplicationUser)
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {

                return NotFound();

            }

            var employees = await _context.Employees
                .Include(e => e.ApplicationUser)
                .Where(e => e.CompanyId == manager.CompanyID.Value && e.IsActive && e.ApplicationUser.IsActive)
                .OrderBy(e => e.ApplicationUser.FirstName)
                .ToListAsync();

            ViewBag.Employees = employees;

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int projectId, int employeeId)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {

                return NotFound();

            }

            var employee = await _context.Employees
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.CompanyId == manager.CompanyID.Value && e.IsActive && e.ApplicationUser.IsActive);

            if (employee == null)
            {

                return NotFound();

            }

            bool exists = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.EmployeeId == employeeId);

            if (exists)
            {
                TempData["ErrorMessage"] = "Employee is already a member of this project.";


                return RedirectToAction("Members", new { id = projectId });
            }

            var member = new ProjectMember
            {
                ProjectId = projectId,

                EmployeeId = employeeId
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

           

            return RedirectToAction("Members", new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int id)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var member = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .FirstOrDefaultAsync(pm => pm.ProjectMemberId == id && pm.Project.CompanyId == manager.CompanyID.Value);

            if (member == null)
            {

                return NotFound();

            }

            int projectId = member.ProjectId;

            // Do not remove employees who still have tasks assigned in this project
            bool hasTasks = await _context.TaskAssignments.AnyAsync(a => a.EmployeeId == member.EmployeeId && a.ProjectTask.ProjectId == projectId);

            if (hasTasks)
            {
                TempData["ErrorMessage"] = "Remove this employee's task assignments before removing them from the project.";

                return RedirectToAction("Members", new { id = projectId });
            }

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();

           
            return RedirectToAction("Members", new { id = projectId });
        }

    }
}
