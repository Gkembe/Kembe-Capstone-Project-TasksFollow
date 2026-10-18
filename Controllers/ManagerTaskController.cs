
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerTaskController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManagerTaskController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ManagerTask/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var company = await _context.Companies.FindAsync(manager.CompanyID.Value);


            if (company == null || !company.IsActive)
            {

                return Forbid();

            }

            var model = new CreateTaskViewModel();

            var projects = await _context.Projects
                .Where(p => p.CompanyId == manager.CompanyID.Value)
                .OrderBy(p => p.Name)
                .ToListAsync();

            model.Projects = projects.Select(p => new SelectListItem
            {
                Value = p.ProjectId.ToString(),
                Text = p.Name

            }).ToList();

            if (projectId.HasValue && projects.Any(p => p.ProjectId == projectId.Value))
            {
                model.ProjectId = projectId.Value;
            }

            return View(model);
        }

        // POST: ManagerTask/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();
            }

            var company = await _context.Companies.FindAsync(manager.CompanyID.Value);

            if (company == null || !company.IsActive)
            {

                return Forbid();
            }
            // Check dates
            if (model.StartDate.HasValue && model.DueDate.HasValue &&  model.DueDate.Value.Date < model.StartDate.Value.Date)
            {

                ModelState.AddModelError("DueDate", "Due date cannot be earlier than start date.");
            }

            // Validate status and priority
            if (model.Status != "Not Started" && model.Status != "In Progress" && model.Status != "Completed")
            {
                ModelState.AddModelError("Status", "Invalid task status.");

            }
                

            if (model.Priority != "Low" && model.Priority != "Medium" && model.Priority != "High")
            {

                ModelState.AddModelError("Priority", "Invalid task priority.");

            }
                

            // Check selected project belongs to manager's company
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {

                ModelState.AddModelError("ProjectId", "Please select a valid project.");

            }
                

            if (!ModelState.IsValid)
            {
                var projects = await _context.Projects
                    .Where(p => p.CompanyId == manager.CompanyID.Value)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                model.Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.ProjectId.ToString(),
                    Text = p.Name

                }).ToList();

                return View(model);
            }

            var task = new ProjectTask
            {
                Title = model.Title,
                Description = model.Description,
                ProjectId = model.ProjectId,
                Status = model.Status,
                Priority = model.Priority,
                Percent = model.Percent,
                StartDate = model.StartDate,
                DueDate = model.DueDate,
                CreatedDate = DateTime.Now,
                CreatedByUserId = manager.Id
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task created successfully.";

            return RedirectToAction("Index");
        }






        public async Task<IActionResult> Index(string? search, int? projectId, string? status = null, string? priority = null, DateTime? dueDate = null)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            int companyId = manager.CompanyID.Value;

            // Get tasks from the manager's company
            var tasks = _context.ProjectTasks
                .Include(t => t.Project)
                .Where(t => t.Project != null && t.Project.CompanyId == companyId)
                .AsQueryable();

            // Search by title or description
            if (!string.IsNullOrWhiteSpace(search))
            {
                tasks = tasks.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));
            }

            // Filter by project
            if (projectId.HasValue)
            {
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                tasks = tasks.Where(t => t.Status == status);
            }

            // Filter by priority
            if (!string.IsNullOrWhiteSpace(priority))
            {
                tasks = tasks.Where(t => t.Priority == priority);
            }

            // Filter by due date
            if (dueDate.HasValue)
            {
                tasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date <= dueDate.Value.Date);
            }

            // Get projects for dropdown
            var projects = await _context.Projects
                .Where(p => p.CompanyId == companyId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Keep selected filters
            ViewBag.Projects = projects;
            ViewBag.Search = search;
            ViewBag.ProjectId = projectId;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.DueDate = dueDate;

            var result = await tasks.OrderByDescending(t => t.CreatedDate).ToListAsync();

            return View(result);
        }





        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();
            }

            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectTaskId == id && t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {

                return NotFound();

            }

            var model = new EditTaskViewModel
            {
                ProjectTaskId = task.ProjectTaskId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                Percent = task.Percent,
                StartDate = task.StartDate,
                DueDate = task.DueDate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTaskViewModel model)
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

                ModelState.AddModelError("Status", "Invalid task status.");
            }
                

            if (model.Priority != "Low" && model.Priority != "Medium" && model.Priority != "High")
            {

                ModelState.AddModelError("Priority", "Invalid task priority.");

            }


            if (!ModelState.IsValid)
            {

                return View(model);
            }

            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectTaskId == model.ProjectTaskId && t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {

                return NotFound();
            }

            task.Title = model.Title;
            task.Description = model.Description;
            task.Status = model.Status;
            task.Priority = model.Priority;
            task.Percent = model.Percent;
            task.StartDate = model.StartDate;
            task.DueDate = model.DueDate;

            await _context.SaveChangesAsync();

          
            return RedirectToAction("Details", new { id = task.ProjectTaskId });
        }



        public async Task<IActionResult> Details(int id)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();
            }

            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Include(t => t.TaskAssignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.ApplicationUser)
                .FirstOrDefaultAsync(t => t.ProjectTaskId == id &&
                    t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {

                return NotFound();
            }

            return View(task);
        }





        [HttpGet]
        public async Task<IActionResult> Assignments(int id)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.ApplicationUser)
                .FirstOrDefaultAsync(t => t.ProjectTaskId == id &&
                    t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {

                return NotFound();
            }

            var members = await _context.ProjectMembers
                .Include(pm => pm.Employee)
                    .ThenInclude(e => e.ApplicationUser)
                .Where(pm => pm.ProjectId == task.ProjectId &&
                    pm.Employee.IsActive && pm.Employee.ApplicationUser.IsActive)
                .ToListAsync();

            ViewBag.Members = members;

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEmployee(int taskId, int employeeId)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();
            }

            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectTaskId == taskId && t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {

                return NotFound();
            }

            bool isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.EmployeeId == employeeId &&
                pm.Employee.CompanyId == manager.CompanyID.Value && pm.Employee.IsActive && pm.Employee.ApplicationUser.IsActive);

            if (!isMember)
            {
                TempData["ErrorMessage"] = "Employee must be an active member of this project.";


                return RedirectToAction("Assignments", new { id = taskId });
            }

            bool exists = await _context.TaskAssignments.AnyAsync(a => a.ProjectTaskId == taskId && a.EmployeeId == employeeId);

            if (exists)
            {
                TempData["ErrorMessage"] = "Employee is already assigned to this task.";

                return RedirectToAction("Assignments", new { id = taskId });
            }

            _context.TaskAssignments.Add(new TaskAssignment
            {
                ProjectTaskId = taskId,

                EmployeeId = employeeId,
                AssignedByUserId = manager.Id,
                AssignedDate = DateTime.Now,
                IsCompleted = false
            });

            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee assigned successfully.";

            return RedirectToAction("Assignments", new { id = taskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignment(int id)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();

            }

            var assignment = await _context.TaskAssignments
                .FirstOrDefaultAsync(a => a.TaskAssignmentId == id &&
                    a.ProjectTask.Project!.CompanyId == manager.CompanyID.Value);

            if (assignment == null)
            {

                return NotFound();
            }

            int taskId = assignment.ProjectTaskId;

            _context.TaskAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task assignment removed successfully.";

            return RedirectToAction("Assignments", new { id = taskId });
        }


    }
}
