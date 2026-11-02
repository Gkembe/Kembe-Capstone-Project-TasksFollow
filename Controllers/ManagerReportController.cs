
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
   
    [Authorize(Roles = "Manager")]
    public class ManagerReportController : Controller
    {
        
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        
        public ManagerReportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ManagerReport/Reports
        // Display project, task, and employee performance reports for the manager's company
        public async Task<IActionResult> Reports()
        {
            // Get the currently logged-in manager
            var user = await _userManager.GetUserAsync(User);

            // Prevent access if the manager is invalid, has no company, or is inactive
            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Get the manager's company ID and today's date
            int companyId = user.CompanyID.Value;
            var today = DateTime.Today;

            // Get all tasks belonging to the manager's company
            // Include project information for each task
            var tasks = await _context.ProjectTasks.Include(t => t.Project).Where(t => t.Project.CompanyId == companyId).ToListAsync();

            // Get all projects belonging to the manager's company
            // Order projects alphabetically by name
            var projects = await _context.Projects.Where(p => p.CompanyId == companyId).OrderBy(p => p.Name).ToListAsync();

            // Create the report ViewModel and calculate summary statistics
            var model = new ManagerReportViewModel
            {
                // Count all projects and tasks
                TotalProjects = projects.Count,
                TotalTasks = tasks.Count,

                // Count completed and in-progress tasks
                CompletedTasks = tasks.Count(t => t.Status == "Completed"),
                InProgressTasks = tasks.Count(t => t.Status == "In Progress"),

                // Count tasks past their due date that are not completed
                OverdueTasks = tasks.Count(t => t.DueDate < today && t.Status != "Completed"),

                // Count active employees belonging to the manager's company
                TotalEmployees = await _context.Employees.CountAsync(e => e.CompanyId == companyId && e.IsActive)
            };

            // Calculate the overall task completion percentage
            // Only calculate when the company has at least one task
            if (model.TotalTasks > 0)
            {
                // Divide completed tasks by total tasks and convert to percentage
                // Round the result to one decimal place
                model.CompletionRate = Math.Round((double)model.CompletedTasks / model.TotalTasks * 100, 1);
            }

            // Calculate performance statistics for each project
            foreach (var project in projects)
            {
                // Get tasks belonging to the current project
                var projectTasks = tasks.Where(t => t.ProjectId == project.ProjectId).ToList();

                // Count completed tasks in this project
                int completed = projectTasks.Count(t => t.Status == "Completed");

                // Set the default completion rate to 0
                double rate = 0;

                // Calculate the project's completion rate if it has tasks
                if (projectTasks.Count > 0)
                {
                    rate = Math.Round((double)completed / projectTasks.Count * 100, 1);
                }

                // Add the project's performance information to the report
                model.Projects.Add(new ProjectReportItem
                {
                    ProjectId = project.ProjectId,
                    ProjectName = project.Name,
                    TotalTasks = projectTasks.Count,
                    CompletedTasks = completed,
                    CompletionRate = rate
                });
            }

            // Send the completed report ViewModel to the Reports view
            return View(model);
        }
    }
}
