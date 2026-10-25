
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

        // GET: EmployeeTask
        public async Task<IActionResult> Index(string? search, string? status, string? priority, DateTime? dueDate)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {

                return Forbid();

            }

            int companyId = user.CompanyID.Value;

            var company = await _context.Companies.FindAsync(companyId);

            if (company == null || !company.IsActive)
            {

                return Forbid();
            }

            // Default status
            if (!Request.Query.ContainsKey("status"))
            {
                status = "In Progress";
            }

            var tasks = _context.TaskAssignments
                .Include(t => t.ProjectTask).ThenInclude(t => t.Project)
                .Where(t => t.Employee.ApplicationUserId == user.Id &&
                            t.Employee.CompanyId == companyId &&
                            t.ProjectTask.Project!.CompanyId == companyId);

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                tasks = tasks.Where(t => t.ProjectTask.Title.Contains(search) || (t.ProjectTask.Description != null && t.ProjectTask.Description.Contains(search)));
            }

            // Status filter

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Completed")
                {
                    tasks = tasks.Where(t => t.IsCompleted);
                }
                else if (status == "In Progress")
                {
                    tasks = tasks.Where(t => !t.IsCompleted &&
                        t.ProjectTask.Status == "In Progress");
                }
                else if (status == "Not Started")
                {
                    tasks = tasks.Where(t => !t.IsCompleted &&
                        t.ProjectTask.Status == "Not Started");
                }
            }


            // Priority filter
            if (!string.IsNullOrWhiteSpace(priority))
            {
                tasks = tasks.Where(t => t.ProjectTask.Priority == priority);
            }

            // Due date filter
            if (dueDate.HasValue)
            {
                var date = dueDate.Value.Date;

                tasks = tasks.Where(t => t.ProjectTask.DueDate >= date && t.ProjectTask.DueDate < date.AddDays(1));
            }

            ViewBag.Search = search;

            ViewBag.Status = status;

            ViewBag.Priority = priority;

            ViewBag.DueDate = dueDate?.ToString("yyyy-MM-dd");

            return View(await tasks.OrderBy(t => t.ProjectTask.DueDate).ToListAsync());
        }

        // GET: EmployeeTask/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {

                return Forbid();
            }

            int companyId = user.CompanyID.Value;

            var assignment = await _context.TaskAssignments.Include(t => t.ProjectTask).ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(t => t.TaskAssignmentId == id && t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == companyId && t.ProjectTask.Project!.CompanyId == companyId);

            if (assignment == null)
            {

                return NotFound();
            }

            return View(assignment);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {

                return Forbid();
            }

            int companyId = user.CompanyID.Value;

            var assignment = await _context.TaskAssignments.Include(t => t.ProjectTask)
                .FirstOrDefaultAsync(t => t.TaskAssignmentId == id && t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == companyId && t.ProjectTask.Project!.CompanyId == companyId && t.ProjectTask.Project.Company!.IsActive);

            if (assignment == null)
            {

                return NotFound();
            }

            assignment.ProjectTask.Percent = 100;

            assignment.IsCompleted = true;

            assignment.CompletDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task completed successfully.";

            return RedirectToAction("Details", new { id });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            int companyId = user.CompanyID.Value;

            var assignment = await _context.TaskAssignments

                .Include(t => t.ProjectTask).FirstOrDefaultAsync(t => t.TaskAssignmentId == id && t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == companyId && t.ProjectTask.Project!.CompanyId == companyId && t.ProjectTask.Project.Company!.IsActive);

            if (assignment == null)
            {

                return NotFound();
            }

            assignment.ProjectTask.Percent = 0;

            assignment.IsCompleted = false;

            assignment.CompletDate = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task reopened successfully.";

            return RedirectToAction("Details", new { id });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProgress(int id, int percent)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {

                return Forbid();
            }

            if (percent < 0 || percent > 100)
            {
                return BadRequest();
            }

            int companyId = user.CompanyID.Value;

            var assignment = await _context.TaskAssignments
                .Include(t => t.ProjectTask)
                .FirstOrDefaultAsync(t => t.TaskAssignmentId == id && t.Employee.ApplicationUserId == user.Id && t.Employee.CompanyId == companyId && t.ProjectTask.Project!.CompanyId == companyId && t.ProjectTask.Project.Company!.IsActive);

            if (assignment == null) return NotFound();

            assignment.ProjectTask.Percent = percent;

            if (percent == 100)
            {
                assignment.IsCompleted = true;


                assignment.CompletDate = DateTime.Now;
            }
            else
            {
                assignment.IsCompleted = false;

                assignment.CompletDate = null;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task progress updated successfully.";

            return RedirectToAction("Details", new { id });
        }



    }
}
