
using System.Runtime.CompilerServices;
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

            var projects = await _context.Projects.Where(p => p.CompanyId == manager.CompanyID.Value).OrderBy(p => p.Name).ToListAsync();

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
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.DueDate.Value.Date < model.StartDate.Value.Date)
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
                var projects = await _context.Projects.Where(p => p.CompanyId == manager.CompanyID.Value).OrderBy(p => p.Name).ToListAsync();

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


        public async Task<IActionResult> Index(string? search, int? projectId, string? status = "In Progress", string? priority = null, DateTime? dueDate = null)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null)
            {
                return Forbid();
            }

            int companyId = manager.CompanyID.Value;

            // Get tasks from the manager's company
            var tasks = _context.ProjectTasks.Include(t => t.Project).Where(t => t.Project != null && t.Project.CompanyId == companyId).AsQueryable();

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
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                tasks = tasks.Where(t => t.Status == status);
            }

            // Filter by priority
            if (!string.IsNullOrWhiteSpace(priority) && priority != "All")
            {
                tasks = tasks.Where(t => t.Priority == priority);
            }

            // Filter by due date
            if (dueDate.HasValue)
            {
                tasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date <= dueDate.Value.Date);
            }

            // Get projects for dropdown
            var projects = await _context.Projects.Where(p => p.CompanyId == companyId).OrderBy(p => p.Name).ToListAsync();

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
            // Get the logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Validate task dates
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.DueDate.Value.Date < model.StartDate.Value.Date)
            {
                ModelState.AddModelError("DueDate", "Due date cannot be earlier than start date.");
            }

            // Validate priority
            if (model.Priority != "Low" && model.Priority != "Medium" && model.Priority != "High")
            {
                ModelState.AddModelError("Priority", "Invalid task priority.");
            }

            // Validate status
            if (model.Status != "Not Started" && model.Status != "In Progress" && model.Status != "Completed")
            {
                ModelState.AddModelError("Status", "Invalid task status.");
            }

            // Validate percentage
            if (model.Percent < 0 || model.Percent > 100)
            {
                ModelState.AddModelError("Percent", "Progress must be between 0 and 100.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get task from manager's company
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectTaskId == model.ProjectTaskId && t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {
                return NotFound();
            }

            // Check which field the manager changed
            bool statusChanged = model.Status != task.Status;
            bool percentChanged = model.Percent != task.Percent;

            // Update task information
            task.Title = model.Title;
            task.Description = model.Description;
            task.Priority = model.Priority;
            task.StartDate = model.StartDate;
            task.DueDate = model.DueDate;

            // If only the status changed, update the percentage automatically
            if (statusChanged && !percentChanged)
            {
                task.Status = model.Status;

                if (model.Status == "Completed")
                {
                    task.Percent = 100;
                }
                else if (model.Status == "Not Started")
                {
                    task.Percent = 0;
                }
                else
                {
                    task.Percent = 50;
                }
            }
            else
            {
                // If percentage changed, use it to determine the status
                task.Percent = model.Percent;

                if (model.Percent == 100)
                {
                    task.Status = "Completed";
                }
                else if (model.Percent == 0)
                {
                    task.Status = "Not Started";
                }
                else
                {
                    task.Status = "In Progress";
                }
            }

            // Get assignments for this task
            var assignments = await _context.TaskAssignments
                .Where(t => t.ProjectTaskId == task.ProjectTaskId)
                .ToListAsync();

            // Synchronize assignment completion with the task status
            foreach (var assignment in assignments)
            {
                if (task.Status == "Completed")
                {
                    assignment.IsCompleted = true;

                    if (assignment.CompletDate == null)
                    {
                        assignment.CompletDate = DateTime.Now;
                    }
                }
                else
                {
                    assignment.IsCompleted = false;
                    assignment.CompletDate = null;
                }
            }

            // Save changes
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task updated successfully.";

            return RedirectToAction("Details", new { id = task.ProjectTaskId });
        }




        public async Task<IActionResult> Details(int id)
        {
            var manager = await _userManager.GetUserAsync(User);


            if (manager == null || manager.CompanyID == null)
            {

                return Forbid();
            }

            var task = await _context.ProjectTasks.Include(t => t.Project).Include(t => t.CreatedByUser).Include(t => t.TaskAssignments).ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.ApplicationUser).FirstOrDefaultAsync(t => t.ProjectTaskId == id && t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {

                return NotFound();
            }

            return View(task);
        }





        [HttpGet]
        public async Task<IActionResult> Assignments(int id)
        {
            // Get the logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Get the task and its assigned employees from the manager's company
            var task = await _context.ProjectTasks.Include(t => t.Project)
                .Include(t => t.TaskAssignments).ThenInclude(a => a.Employee).ThenInclude(e => e.ApplicationUser).FirstOrDefaultAsync(t => t.ProjectTaskId == id && t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {
                return NotFound();
            }

            // Get active project members who can be assigned to this task
            var members = await _context.ProjectMembers.Include(pm => pm.Employee)
                    .ThenInclude(e => e.ApplicationUser).Where(pm => pm.ProjectId == task.ProjectId && pm.Employee.CompanyId == manager.CompanyID.Value && pm.Employee.IsActive && pm.Employee.ApplicationUser.IsActive).ToListAsync();

            ViewBag.Members = members;

            return View(task);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEmployee(int taskId, int employeeId)
        {
            // Get the logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find the task in the manager's company
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectTaskId == taskId && t.Project!.CompanyId == manager.CompanyID.Value);

            if (task == null)
            {
                return NotFound();
            }

            // Verify that the employee is an active member of the project
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.CompanyId == manager.CompanyID.Value &&  e.IsActive && e.ApplicationUser.IsActive && _context.ProjectMembers.Any(pm => pm.ProjectId == task.ProjectId && pm.EmployeeId == e.EmployeeId));

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee must be an active member of this project.";
                return RedirectToAction("Assignments", new { id = taskId });
            }

            // Prevent assigning the same employee twice
            bool exists = await _context.TaskAssignments.AnyAsync(a => a.ProjectTaskId == taskId && a.EmployeeId == employeeId);

            if (exists)
            {
                TempData["ErrorMessage"] = "Employee is already assigned to this task.";
                return RedirectToAction("Assignments", new { id = taskId });
            }

            // Create the task assignment
            _context.TaskAssignments.Add(new TaskAssignment
            {
                ProjectTaskId = taskId,
                EmployeeId = employeeId,
                AssignedByUserId = manager.Id,
                AssignedDate = DateTime.Now,
                IsCompleted = false
            });

            // Notify the employee about the new assignment
            _context.Notifications.Add(new Notification
            {
                UserId = employee.ApplicationUserId,
                Title = "New Task Assigned",
                Message = "You have been assigned to the task: " + task.Title + "that has been created on " + task.CreatedDate + " and due on " + task.DueDate,
                ProjectTaskId = task.ProjectTaskId,
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            // Save the assignment and notification together
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee assigned successfully.";

            return RedirectToAction("Assignments", new { id = taskId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignment(int id)
        {
            // Get the logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find the assignment in the manager's company
            var assignment = await _context.TaskAssignments.Include(a => a.Employee).Include(a => a.ProjectTask).FirstOrDefaultAsync(a => a.TaskAssignmentId == id && a.ProjectTask.Project!.CompanyId == manager.CompanyID.Value);

            if (assignment == null)
            {
                return NotFound();
            }

            int taskId = assignment.ProjectTaskId;

            // Notify the employee that they have been removed from the task
            _context.Notifications.Add(new Notification
            {
                UserId = assignment.Employee.ApplicationUserId,
                Title = "Task Assignment Removed",
                Message = "You have been removed from the task: " + assignment.ProjectTask.Title,
                ProjectTaskId = taskId,
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            // Remove the assignment
            _context.TaskAssignments.Remove(assignment);

            // Save the removal and notification together
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task assignment removed successfully.";

            return RedirectToAction("Assignments", new { id = taskId });
        }


        //***********************************Comments ***********************************



        // Display comments and replies for a selected task

        [HttpGet]
        public async Task<IActionResult> Comments(int id)
        {
            // Get the logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();
            }

            // Find the task and make sure it belongs to the manager's company
            var task = await _context.ProjectTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.ProjectTaskId == id && t.Project.CompanyId == user.CompanyID.Value);

            if (task == null)
            {
                return NotFound();
            }

            // Get all comments and replies for the selected task
            var comments = await _context.Comments.Include(c => c.User).Where(c => c.ProjectTaskId == id).OrderBy(c => c.CreatedDate).ToListAsync();

            // Get parent comment IDs that have active replies
            var parentIds = comments.Where(c => !c.IsDeleted && c.ParentCommentId != null).Select(c => c.ParentCommentId.Value).ToList();

            // Hide deleted comments unless they are parent comments with active replies
            var visibleComments = comments.Where(c => !c.IsDeleted || (c.ParentCommentId == null && parentIds.Contains(c.CommentId))).ToList();

            // Send task information and logged-in user ID to the view
            ViewBag.Task = task;
            ViewBag.CurrentUserId = user.Id;

            return View(visibleComments);
        }





        // Add a new comment to a task
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int taskId, string content)
        {
            // Get the logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();
            }

            // Check that the task belongs to the manager's company
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectTaskId == taskId && t.Project.CompanyId == user.CompanyID.Value);

            if (task == null)
            {
                return NotFound();
            }

            // Validate the comment content
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
            {
                TempData["ErrorComment"] = "Comment must contain between 1 and 2000 characters.";

                return RedirectToAction("Comments", new { id = taskId });
            }

            // Create a new comment
            var comment = new Comment
            {
                Content = content.Trim(),
                ProjectTaskId = taskId,
                UserId = user.Id,
                CreatedDate = DateTime.Now,
                IsDeleted = false
            };

            // Save the comment to the database
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Display a success message
            TempData["SuccessComment"] = "Comment added successfully.";

            return RedirectToAction("Comments", new { id = taskId });
        }




        // Reply to an existing comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyComment(int commentId, string content)
        {
            // Get the logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();
            }

            // Find the parent comment and its related task and project
            var parent = await _context.Comments.Include(c => c.ProjectTask).ThenInclude(t => t.Project).FirstOrDefaultAsync(c => c.CommentId == commentId && !c.IsDeleted);

            if (parent == null)
            {
                return NotFound();
            }

            // Make sure the parent comment belongs to the manager's company
            if (parent.ProjectTask.Project.CompanyId != user.CompanyID.Value)
            {
                return Forbid();
            }

            // Validate the reply content
            if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > 2000)
            {
                TempData["ErrorReply"] = "Reply must contain between 1 and 2000 characters.";
                return RedirectToAction("Comments", new { id = parent.ProjectTaskId });
            }

            // Create a reply linked to the parent comment
            var reply = new Comment
            {
                Content = content.Trim(),
                ProjectTaskId = parent.ProjectTaskId,
                UserId = user.Id,
                ParentCommentId = parent.CommentId,
                CreatedDate = DateTime.Now
            };

            // Save the reply to the database
            _context.Comments.Add(reply);
            await _context.SaveChangesAsync();

            // Display a success message
            TempData["SuccessReply"] = "Reply added successfully.";

            return RedirectToAction("Comments", new { id = parent.ProjectTaskId });
        }




        // Soft delete a comment or reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            // Get the logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();
            }

            // Find the comment and load its related task and project
            var comment = await _context.Comments.Include(c => c.ProjectTask).ThenInclude(t => t.Project).FirstOrDefaultAsync(c => c.CommentId == id);

            if (comment == null)
            {
                return NotFound();
            }

            // Make sure the comment belongs to the manager's company
            if (comment.ProjectTask.Project.CompanyId != user.CompanyID.Value)
            {
                return Forbid();
            }

            // Only the comment author can delete their comment
            if (comment.UserId != user.Id)
            {
                return Forbid();
            }

            // Mark the comment as deleted without removing it from the database
            comment.IsDeleted = true;

            // Save the change to the database
            await _context.SaveChangesAsync();

            // Display a success message
            TempData["DeleteComment"] = "Comment deleted successfully.";

            return RedirectToAction("Comments", new { id = comment.ProjectTaskId });
        }


    }
}
