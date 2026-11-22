using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;
using TasksFollow.Services;

namespace TasksFollow.Controllers
{
   
    [Authorize(Roles = "Manager")]
    public class ManagerProjectController : Controller
    {
      
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly EmailService _emailService;

        public ManagerProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: ManagerProject/Index
        // List projects in the manager's company with optional search and date filters.
        public async Task<IActionResult> Index(string? search, string? status, DateTime? startDate, DateTime? dueDate)
        {
            // Identify the signed-in manager before accessing company information.
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            // Use the manager's company ID to keep project data within their workspace.
            int companyId = manager.CompanyID.Value;

            // Start with projects belonging to the manager's company.
            var projects = _context.Projects.Include(p => p.CreatedByUser).Where(p => p.CompanyId == companyId).AsQueryable();

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

            // Load matching projects with the most recently created IDs first.
            var result = await projects.OrderByDescending(p => p.ProjectId).ToListAsync();

            // Keep the selected filters when the Index page reloads.
            ViewBag.Search = search;
            ViewBag.Status = status;

            ViewBag.StartDate = startDate;
            ViewBag.DueDate = dueDate;

            return View(result);
        }



        // GET: ManagerProject/Create
        // Show the form for creating a project.
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateProjectViewModel());
        }

        // POST: ManagerProject/Create
        // Validate the form and save a new project for the manager's company.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectViewModel model)
        {
            // Validate dates
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.DueDate.Value.Date < model.StartDate.Value.Date)
            {
                ModelState.AddModelError("DueDate", "Due date cannot be earlier than start date.");
            }

            // Redisplay the form when submitted values fail validation.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get logged-in manager
            // Identify the signed-in manager before accessing company information.
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            // Check company
            // Allow project creation only for an existing, active company.
            var company = await _context.Companies.FindAsync(manager.CompanyID.Value);

            if (company == null || !company.IsActive)
            {
                return Forbid();
            }

            // Validate status
            // Only accept one of the supported project statuses.
            if (model.Status != "Not Started" && model.Status != "In Progress" && model.Status != "Completed")
            {
                ModelState.AddModelError("Status", "Please select a valid project status.");

                return View(model);
            }

            // Create project
            // Build the project using validated form fields and the manager's identity.
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

            // Add the new project and save it to the database.
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }



        // GET: ManagerProject/Edit/5
        // Show the edit form only for a project in the manager's company.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Identify the signed-in manager before accessing company information.
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

            // Copy the current project details into the edit form.
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
        // Validate submitted changes and update the existing project.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProjectViewModel model)
        {
            // Identify the signed-in manager before accessing company information.
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            if (model.StartDate.HasValue && model.DueDate.HasValue && model.DueDate.Value.Date < model.StartDate.Value.Date)
            {
                ModelState.AddModelError("DueDate", "Due date cannot be earlier than start date.");
            }

            // Only accept one of the supported project statuses.
            if (model.Status != "Not Started" && model.Status != "In Progress" && model.Status != "Completed")
            {
                ModelState.AddModelError("Status", "Please select a valid status.");
            }

            // Redisplay the form when submitted values fail validation.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {
                return NotFound();
            }

            // Update the editable project fields.
            project.Name = model.Name;
            project.Description = model.Description;
            project.StartDate = model.StartDate;
            project.DueDate = model.DueDate;
            project.Status = model.Status;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = project.ProjectId });
        }

        // GET: ManagerProject/Details/5
        // Show a project with its creator, members, and tasks.
        public async Task<IActionResult> Details(int id)
        {
            // Identify the signed-in manager before accessing company information.
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            var project = await _context.Projects.Include(p => p.CreatedByUser).Include(p => p.ProjectMembers).Include(p => p.ProjectTask).FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }



        // Display project members and available active employees.
        [HttpGet]
        public async Task<IActionResult> Members(int id)
        {
            // Identify the signed-in manager before accessing company information.
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            var project = await _context.Projects.Include(p => p.ProjectMembers).ThenInclude(pm => pm.Employee).ThenInclude(e => e.ApplicationUser).FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {
                return NotFound();
            }

            // Load active employees from the same company for the member selection list.
            var employees = await _context.Employees.Include(e => e.ApplicationUser).Where(e => e.CompanyId == manager.CompanyID.Value && e.IsActive && e.ApplicationUser.IsActive).OrderBy(e => e.ApplicationUser.FirstName).ToListAsync();

            // Pass eligible employees to the Members view.
            ViewBag.Employees = employees;

            return View(project);
        }



        // Add an eligible employee to this project if not already a member.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int projectId, int employeeId)
        {
            // Get the logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find the project in the manager's company
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CompanyId == manager.CompanyID.Value);

            if (project == null)
            {
                return NotFound();
            }

            // Find the active employee
            var employee = await _context.Employees.Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.CompanyId == manager.CompanyID.Value && e.IsActive && e.ApplicationUser.IsActive);

            if (employee == null)
            {
                return NotFound();
            }

            // Prevent duplicate project memberships
            bool exists = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.EmployeeId == employeeId);

            if (exists)
            {
                TempData["ErrorMessage"] = "Employee is already a member of this project.";
                return RedirectToAction("Members", new { id = projectId });
            }

            // Add the employee to the project
            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                EmployeeId = employeeId
            });

            // Create in-app notification
            _context.Notifications.Add(new Notification
            {
                UserId = employee.ApplicationUserId,
                Title = "Added to Project",
                Message = "You have been added to the project: " + project.Name,
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            // Save membership and notification
            await _context.SaveChangesAsync();

            // Send email notification
            if (!string.IsNullOrWhiteSpace(employee.ApplicationUser.Email))
            {
                try
                {
                    string subject = "Added to Project - TasksFollow";

                    string body = "Hello " + employee.ApplicationUser.FirstName + ",\n\n" +
                                  "You have been added to a new project.\n\n" +
                                  "Project: " + project.Name + "\n\n" +
                                  "You can now view this project and its assigned tasks in TasksFollow.\n\n" +
                                  "Please log in to TasksFollow for more information.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(employee.ApplicationUser.Email, subject, body);
                }
                catch (Exception)
                {
                    // Email failure should not cancel the project membership
                }
            }

            TempData["SuccessMessage"] = "Employee added to the project successfully.";
            return RedirectToAction("Members", new { id = projectId });
        }




        // Remove an employee from a project only when they have no assigned project tasks.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int id)
        {
            // Get the logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find the project member in the manager's company
            var member = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .Include(pm => pm.Employee).ThenInclude(e => e.ApplicationUser)
                .FirstOrDefaultAsync(pm => pm.ProjectMemberId == id && pm.Project.CompanyId == manager.CompanyID.Value);

            if (member == null)
            {
                return NotFound();
            }

            int projectId = member.ProjectId;

            // Prevent removing employees with assigned tasks
            bool hasTasks = await _context.TaskAssignments
                .AnyAsync(a => a.EmployeeId == member.EmployeeId && a.ProjectTask.ProjectId == projectId);

            if (hasTasks)
            {
                TempData["ErrorMessage"] = "Remove this employee's task assignments before removing them from the project.";
                return RedirectToAction("Members", new { id = projectId });
            }

            // Create in-app notification
            _context.Notifications.Add(new Notification
            {
                UserId = member.Employee.ApplicationUserId,
                Title = "Removed from Project",
                Message = "You have been removed from the project: " + member.Project.Name,
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            // Remove project membership
            _context.ProjectMembers.Remove(member);

            // Save removal and notification
            await _context.SaveChangesAsync();

            // Send email notification
            if (!string.IsNullOrWhiteSpace(member.Employee.ApplicationUser.Email))
            {
                try
                {
                    string subject = "Removed from Project - TasksFollow";

                    string body = "Hello " + member.Employee.ApplicationUser.FirstName + ",\n\n" +
                                  "You have been removed from the following project:\n\n" +
                                  "Project: " + member.Project.Name + "\n\n" +
                                  "You are no longer a member of this project.\n\n" +
                                  "Please log in to TasksFollow for more information.\n\n" +
                                  "TasksFollow Team";

                    await _emailService.SendEmailAsync(member.Employee.ApplicationUser.Email, subject, body);
                }
                catch (Exception)
                {
                    // Email failure should not cancel the project membership removal
                }
            }

            TempData["SuccessMessage"] = "Employee removed from the project successfully.";

            return RedirectToAction("Members", new { id = projectId });
        }

    }
}
