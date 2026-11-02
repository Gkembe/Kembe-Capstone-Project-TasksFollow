
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{

    [Authorize(Roles = "Employee")]
    public class EmployeeNotificationController : Controller
    {
      
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

     
        public EmployeeNotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: EmployeeNotification/Index
        // Display all notifications belonging to the logged-in employee
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get the currently logged-in employee
            var user = await _userManager.GetUserAsync(User);

            // Prevent access if the user is invalid, has no company, or is inactive
            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Get notifications belonging to this employee
            // Show unread notifications first, followed by the newest notifications
            var notifications = await _context.Notifications.Where(n => n.UserId == user.Id).OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedDate).ToListAsync();

            // Send the notification list to the Index view
            return View(notifications);
        }



        // POST: EmployeeNotification/MarkAsRead
        // Mark a single notification as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // Get the currently logged-in employee
            var user = await _userManager.GetUserAsync(User);

            // Verify the employee is active and belongs to a company
            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Find the notification and make sure it belongs to this employee
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == user.Id);

            // Return NotFound if the notification does not exist
            if (notification == null)
            {
                return NotFound();
            }

            // Only update the notification if it has not been read
            if (!notification.IsRead)
            {
                // Mark the notification as read and record the date
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;

                // Save the changes to the database
                await _context.SaveChangesAsync();
            }

            // Return to the employee notification list
            return RedirectToAction("Index");
        }



        // POST: EmployeeNotification/MarkAllAsRead
        // Mark all unread notifications belonging to the employee as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            // Get the currently logged-in employee
            var user = await _userManager.GetUserAsync(User);

            // Verify the employee is active and belongs to a company
            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Get all unread notifications belonging to this employee
            var notifications = await _context.Notifications.Where(n => n.UserId == user.Id && !n.IsRead).ToListAsync();

            // Mark each notification as read and record the date
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
            }

            // Save all notification updates to the database
            await _context.SaveChangesAsync();

            // Display a success message
            TempData["SuccessMessage"] = "All notifications marked as read.";

            // Return to the employee notification list
            return RedirectToAction("Index");
        }




        // POST: EmployeeNotification/OpenTask
        // Open an assigned task when an employee clicks its notification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenTask(int id)
        {
            // Get the currently logged-in employee
            var user = await _userManager.GetUserAsync(User);

            // Verify the employee is active and belongs to a company
            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Find the notification and verify it belongs to this employee
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == user.Id);

            // Return NotFound if the notification does not exist
            if (notification == null)
            {
                return NotFound();
            }

            // Mark the notification as read before opening the task
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;

                // Save the notification update
                await _context.SaveChangesAsync();
            }

            // Only open notifications for newly assigned tasks
            // Return to Index if the notification is not linked to a task
            if (notification.Title != "New Task Assigned" || notification.ProjectTaskId == null)
            {
                return RedirectToAction("Index");
            }

            // Verify the employee is still assigned to this task
            // Also verify the employee and task belong to the same company
            bool assigned = await _context.TaskAssignments.AnyAsync(a => a.ProjectTaskId == notification.ProjectTaskId.Value && a.Employee.ApplicationUserId == user.Id && a.Employee.CompanyId == user.CompanyID.Value && a.ProjectTask.Project!.CompanyId == user.CompanyID.Value);

            // Prevent access if the employee is no longer assigned to the task
            if (!assigned)
            {
                TempData["ErrorMessage"] = "You are no longer assigned to this task.";
                return RedirectToAction("Index");
            }

            // Redirect the employee to the assigned task details page
            return RedirectToAction("Details", "EmployeeTask", new { id = notification.ProjectTaskId.Value });
        }
    }
}
