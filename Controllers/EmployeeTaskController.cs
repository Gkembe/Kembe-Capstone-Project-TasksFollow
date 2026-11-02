
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    
    [Authorize(Roles = "Employee")]
    public class EmployeeTaskController : Controller
    {
       
        private readonly ApplicationDbContext _context;
       
        private readonly UserManager<ApplicationUser> _userManager;

    
        public EmployeeTaskController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // GET: EmployeeTask/Index
        // Show the employee's assigned tasks with optional search and filters
        public async Task<IActionResult> Index(string? search, string? status, string? priority, DateTime? dueDate)
        {
            // Get the logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            int companyId = user.CompanyID.Value;

            // Check whether the company is active
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null || !company.IsActive)
            {
                return Forbid();
            }

            // Set In Progress as the default status
            if (!Request.Query.ContainsKey("status"))
            {
                status = "In Progress";
            }

            // Get tasks assigned to the logged-in employee
            var tasks = _context.TaskAssignments.Include(t => t.ProjectTask).ThenInclude(t => t.Project).Where(t => t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == companyId && t.ProjectTask.Project!.CompanyId == companyId);

            // Search by task title or description
            if (!string.IsNullOrWhiteSpace(search))
            {
                tasks = tasks.Where(t => t.ProjectTask.Title.Contains(search) || (t.ProjectTask.Description != null && t.ProjectTask.Description.Contains(search)));
            }

            // Filter by task status
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                tasks = tasks.Where(t => t.ProjectTask.Status == status);
            }

            // Filter by priority
            if (!string.IsNullOrWhiteSpace(priority) && priority != "All")
            {
                tasks = tasks.Where(t => t.ProjectTask.Priority == priority);
            }

            // Filter by due date
            if (dueDate.HasValue)
            {
                var date = dueDate.Value.Date;

                tasks = tasks.Where(t => t.ProjectTask.DueDate >= date && t.ProjectTask.DueDate < date.AddDays(1));
            }

            // Keep selected filters
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.DueDate = dueDate?.ToString("yyyy-MM-dd");

            // Return filtered assignments
            return View(await tasks.OrderBy(t => t.ProjectTask.DueDate).ToListAsync());
        }


        // GET: EmployeeTask/Details/{id}
        // Show a specific assignment only if it belongs to this employee
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Get logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Get assignment with task and project
            var assignment = await _context.TaskAssignments.Include(t => t.ProjectTask).ThenInclude(t => t.Project).FirstOrDefaultAsync(t => t.TaskAssignmentId == id && t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == user.CompanyID.Value && t.ProjectTask.Project!.CompanyId == user.CompanyID.Value && t.ProjectTask.Project.Company!.IsActive);

            if (assignment == null)
            {
                return NotFound();
            }

            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Complete this employee's assignment and notify its assigning manager
        public async Task<IActionResult> Complete(int id)
        {
            // Get the logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            int companyId = user.CompanyID.Value;

            // Get the employee's assignment
            var assignment = await _context.TaskAssignments.Include(t => t.ProjectTask).FirstOrDefaultAsync(t => t.TaskAssignmentId == id && t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == companyId && t.ProjectTask.Project!.CompanyId == companyId && t.ProjectTask.Project.Company!.IsActive);

            if (assignment == null)
            {
                return NotFound();
            }

            // Prevent duplicate completion notifications
            if (assignment.IsCompleted)
            {
                return RedirectToAction("Details", new { id });
            }

            // Mark the employee's assignment as completed
            assignment.IsCompleted = true;

            assignment.CompletDate = DateTime.Now;

            // Check whether all assigned employees have completed their assignments
            bool allCompleted = await _context.TaskAssignments.Where(t => t.ProjectTaskId == assignment.ProjectTaskId).AllAsync(t => t.TaskAssignmentId == assignment.TaskAssignmentId || t.IsCompleted);

            // Update the overall task status
            if (allCompleted)
            {
                assignment.ProjectTask.Percent = 100;
                assignment.ProjectTask.Status = "Completed";
            }
            else
            {
                assignment.ProjectTask.Status = "In Progress";
            }

            // Notify the manager who assigned the task
            bool managerExists = await _context.Users.AnyAsync(u => u.Id == assignment.AssignedByUserId && u.CompanyID == companyId && u.IsActive);

            if (managerExists)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = assignment.AssignedByUserId,
                    Title = "Task Completed",
                    Message = user.FirstName + " " + user.LastName + " completed the assignment: " + assignment.ProjectTask.Title + " on " + assignment.CompletDate,
                    ProjectTaskId = assignment.ProjectTaskId,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
            }

            // Save changes
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task marked as completed.";

            return RedirectToAction("Details", new { id });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        // Reopen a previously completed assignment and notify its assigning manager
        public async Task<IActionResult> Reopen(int id)
        {
            // Get the logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            int companyId = user.CompanyID.Value;

            // Get the employee's assignment
            var assignment = await _context.TaskAssignments.Include(t => t.ProjectTask).FirstOrDefaultAsync(t => t.TaskAssignmentId == id && t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == companyId && t.ProjectTask.Project!.CompanyId == companyId && t.ProjectTask.Project.Company!.IsActive);

            if (assignment == null)
            {
                return NotFound();
            }

            // Check whether the assignment is already open
            if (!assignment.IsCompleted)
            {
                return RedirectToAction("Details", new { id });
            }

            // Reopen the employee's assignment
            assignment.IsCompleted = false;
            assignment.CompletDate = null;

            // Update the task status
            assignment.ProjectTask.Status = "In Progress";

            if (assignment.ProjectTask.Percent == 100)
            {
                assignment.ProjectTask.Percent = 99;
            }

            // Notify the manager
            bool managerExists = await _context.Users.AnyAsync(u => u.Id == assignment.AssignedByUserId && u.CompanyID == companyId && u.IsActive);

            if (managerExists)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = assignment.AssignedByUserId,
                    Title = "Task Reopened",
                    Message = user.FirstName + " " + user.LastName + " reopened the assignment: " + assignment.ProjectTask.Title,
                    ProjectTaskId = assignment.ProjectTaskId,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
            }

            // Save changes
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task reopened successfully.";

            return RedirectToAction("Details", new { id });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        // Change task progress between 0 and 100 and update completion information
        public async Task<IActionResult> UpdateProgress(int id, int percent)
        {
            // Get the logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Validate progress percentage
            if (percent < 0 || percent > 100)
            {
                return BadRequest();
            }

            int companyId = user.CompanyID.Value;

            // Get the employee's assignment
            var assignment = await _context.TaskAssignments.Include(t => t.ProjectTask).FirstOrDefaultAsync(t => t.TaskAssignmentId == id && t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == companyId && t.ProjectTask.Project!.CompanyId == companyId && t.ProjectTask.Project.Company!.IsActive);

            if (assignment == null)
            {
                return NotFound();
            }

            // Remember the previous progress and completion status
            int oldPercent = assignment.ProjectTask.Percent;
            bool wasCompleted = assignment.IsCompleted;

            // Avoid unnecessary updates and duplicate notifications
            if (oldPercent == percent && wasCompleted == (percent == 100))
            {
                return RedirectToAction("Details", new { id });
            }

            // Update task progress
            assignment.ProjectTask.Percent = percent;

            // Update task and assignment status
            if (percent == 100)
            {
                assignment.ProjectTask.Status = "Completed";
                assignment.IsCompleted = true;

                if (assignment.CompletDate == null)
                {
                    assignment.CompletDate = DateTime.Now;
                }
            }
            else
            {
                assignment.ProjectTask.Status = "In Progress";
                assignment.IsCompleted = false;
                assignment.CompletDate = null;
            }

            // Determine the notification type
            string title = "Task Progress Updated";
            string message = user.FirstName + " " + user.LastName + " updated the progress of " + assignment.ProjectTask.Title + " from " + oldPercent + "% to " + percent + "%.";

            if (percent == 100 && !wasCompleted)
            {
                title = "Task Completed";

                message = user.FirstName + " " + user.LastName + " completed the assignment: " + assignment.ProjectTask.Title + " on " + assignment.CompletDate;
            }
            else if (wasCompleted && percent < 100)
            {
                title = "Task Reopened";
                message = user.FirstName + " " + user.LastName + " reopened the assignment: " + assignment.ProjectTask.Title;
            }

            // Notify the manager
            bool managerExists = await _context.Users.AnyAsync(u => u.Id == assignment.AssignedByUserId && u.CompanyID == companyId && u.IsActive);

            if (managerExists)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = assignment.AssignedByUserId,
                    Title = title,
                    Message = message,
                    ProjectTaskId = assignment.ProjectTaskId,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
            }

            // Save changes
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task progress updated successfully.";

            return RedirectToAction("Details", new { id });
        }




        // ALL COMPANY TASKS

        // Display all tasks belonging to the employee's company, not just assigned tasks
        [HttpGet]
        public async Task<IActionResult> AllTasks(string search, string status, string priority)
        {
            // Get the logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();
            }

            // Get all tasks from the employee's company
            var tasks = _context.ProjectTasks.Include(t => t.Project).Where(t => t.Project.CompanyId == user.CompanyID.Value);

            // Search tasks by title
            if (!string.IsNullOrWhiteSpace(search))
            {
                tasks = tasks.Where(t => t.Title.Contains(search));
            }

            // Filter tasks by status
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                tasks = tasks.Where(t => t.Status == status);
            }

            // Filter tasks by priority
            if (!string.IsNullOrWhiteSpace(priority) && priority != "All")
            {
                tasks = tasks.Where(t => t.Priority == priority);
            }

            // Keep selected filters
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;

            // Return tasks ordered by due date
            return View(await tasks.OrderBy(t => t.DueDate).ToListAsync());
        }



        // TASK COMMENTS AND REPLIES

        // Display visible comments and replies for a task in the employee's company

        [HttpGet]
        public async Task<IActionResult> Comments(int id)
        {
            // Get the logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();
            }

            // Find the task and make sure it belongs to the employee's company
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



        // Add a comment to a task belonging to the employee's company
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int taskId, string content)
        {
            // Get the logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();
            }

            // Check that the task belongs to the employee's company
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



        // Reply to an existing, non-deleted comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyComment(int commentId, string content)
        {
            // Get the logged-in employee
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

            // Make sure the parent comment belongs to the employee's company
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



        // Soft delete the employee's own comment or reply without erasing its record
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            // Get the logged-in employee
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

            // Make sure the comment belongs to the employee's company
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

        // Display details of any task belonging to the employee's company
        [HttpGet]
        public async Task<IActionResult> AllTaskDetails(int id)
        {
            // Get the logged-in employee
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();
            }

            // Get the task and its project
            var task = await _context.ProjectTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.ProjectTaskId == id && t.Project.CompanyId == user.CompanyID.Value);

            if (task == null)
            {
                return NotFound();
            }

            // Check whether the employee is assigned to this task
            ViewBag.IsAssigned = await _context.TaskAssignments.AnyAsync(a => a.ProjectTaskId == id && a.Employee.ApplicationUserId == user.Id);

            return View(task);
        }
    }
}
