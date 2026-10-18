
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManagerDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }


        public async Task<IActionResult> Index()
        {
            // Get logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null)
            {
                return Forbid();

            }

            int companyId = user.CompanyID.Value;

            // Get company
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null || !company.IsActive)
            {

                return Forbid();
            }

            var model = new ManagerDashboardViewModel
            {
                CompanyName = company.Name,

                // Employees
                TotalEmployees = await _context.Employees.CountAsync(e => e.CompanyId == companyId),

                ActiveEmployees = await _context.Employees.CountAsync(e => e.CompanyId == companyId && e.IsActive),

                InactiveEmployees = await _context.Employees.CountAsync(e => e.CompanyId == companyId && !e.IsActive),

                // Projects
                TotalProjects = await _context.Projects.CountAsync(p => p.CompanyId == companyId),

                ActiveProjects = await _context.Projects.CountAsync(p => p.CompanyId == companyId && p.Status == "In Progress"),

                CompletedProjects = await _context.Projects.CountAsync(p => p.CompanyId == companyId && p.Status == "Completed"),

                // Tasks
                TotalTasks = await _context.ProjectTasks.CountAsync(t => t.Project!.CompanyId == companyId),

                PendingTasks = await _context.ProjectTasks.CountAsync(t => t.Project!.CompanyId == companyId && t.Status == "Not Started"),

                InProgressTasks = await _context.ProjectTasks.CountAsync(t => t.Project!.CompanyId == companyId && t.Status == "In Progress"),

                CompletedTasks = await _context.ProjectTasks.CountAsync(t => t.Project!.CompanyId == companyId && t.Status == "Completed"),

                // Overdue tasks
                OverdueTasks = await _context.ProjectTasks.CountAsync(t => t.Project!.CompanyId == companyId && t.DueDate.HasValue && t.DueDate.Value < DateTime.Today && t.Status != "Completed")
            };

            return View(model);
        }

    }
}
