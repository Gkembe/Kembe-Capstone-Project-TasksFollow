
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: EmployeeProject
        public async Task<IActionResult> Index(string? search, string? status)
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

            if (!Request.Query.ContainsKey("status"))
            {
                status = "In Progress";
            }

            var projects = _context.Projects.Where(p => p.CompanyId == companyId && p.ProjectMembers.Any(m => m.Employee.ApplicationUserId == user.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                projects = projects.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                projects = projects.Where(p => p.Status == status);
            }

            ViewBag.Search = search;

            ViewBag.Status = status;

            return View(await projects.OrderBy(p => p.DueDate).ToListAsync());
        }




        // GET: EmployeeProject/Details/5
        public async Task<IActionResult> Details(int id)
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

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyId == companyId && p.ProjectMembers.Any(m => m.Employee.ApplicationUserId == user.Id));

            if (project == null) return NotFound();

            ViewBag.MyAssignments = await _context.TaskAssignments.Include(a => a.ProjectTask).Where(a => a.Employee.ApplicationUserId == user.Id && a.Employee.CompanyId == companyId && a.ProjectTask.ProjectId == id).OrderBy(a => a.ProjectTask.DueDate).ToListAsync();

            return View(project);
        }
    }
}
