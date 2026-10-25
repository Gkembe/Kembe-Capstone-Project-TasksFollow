using System.ComponentModel.Design;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    [Authorize (Roles = "Employee")]
    public class EmployeeDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {

            //Get loged in employee

            var user = await _userManager.GetUserAsync(User);


            if (user == null || user.CompanyID == null)
            {
                return Forbid();

            }

            int companyID = user.CompanyID.Value;

            // Get company
            var company = await _context.Companies.FindAsync(companyID);

            if (company == null || !company.IsActive)
            {

                return Forbid();
            }

            var model = new EmployeeDashboardViewModel
            {

                CompanyName = company.Name,


                TotalCompletedTasks = await _context.TaskAssignments.CountAsync(t => t.Employee.ApplicationUserId == user.Id && t.IsCompleted == true),

                TotalAssignedTasks = await _context.TaskAssignments.CountAsync(t => t.Employee.ApplicationUserId == user.Id),

                TotalInProgressTasks = await _context.TaskAssignments.CountAsync(t => t.Employee.ApplicationUserId == user.Id && t.ProjectTask.Status == "In Progress"),

                OverDueTasks = await _context.TaskAssignments.CountAsync(t => t.Employee.ApplicationUserId == user.Id && t.ProjectTask.DueDate < DateTime.Today && !t.IsCompleted),

            };


            return View(model);
        }
    }
}
