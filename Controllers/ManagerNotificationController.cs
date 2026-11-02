
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    
    [Authorize(Roles = "Manager")]
    public class ManagerNotificationController : Controller
    {
       
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

       
        public ManagerNotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ManagerNotification/Index
        // Display notifications belonging to the logged-in manager
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get the currently logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            // Prevent access if the manager is invalid, has no company, or is inactive
            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Get notifications belonging to this manager
            // Display unread notifications first, followed by the newest notifications
            var notifications = await _context.Notifications.Where(n => n.UserId == manager.Id).OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedDate).ToListAsync();

            // Send the notification list to the Index view
            return View(notifications);
        }


        // POST: ManagerNotification/MarkAsRead
        // Mark a single notification as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // Get the currently logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            // Verify the manager is active and belongs to a company
            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find the notification and verify it belongs to this manager
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == manager.Id);

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

            // Return to the manager notification list
            return RedirectToAction("Index");
        }



        // POST: ManagerNotification/MarkAllAsRead
        // Mark all unread notifications belonging to the manager as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            // Get the currently logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            // Verify the manager is active and belongs to a company
            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Get all unread notifications belonging to this manager
            var notifications = await _context.Notifications.Where(n => n.UserId == manager.Id && !n.IsRead).ToListAsync();

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

            // Return to the manager notification list
            return RedirectToAction("Index");
        }


        // POST: ManagerNotification/OpenTask
        // Open the task related to a notification and mark the notification as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenTask(int id)
        {
            // Get the currently logged-in manager
            var manager = await _userManager.GetUserAsync(User);

            // Verify the manager is active and belongs to a company
            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            // Find the notification and verify it belongs to this manager
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == manager.Id);

            // Return NotFound if the notification does not exist
            if (notification == null)
            {
                return NotFound();
            }

            // Make sure the notification is linked to a task
            if (notification.ProjectTaskId == null)
            {
                TempData["ErrorMessage"] = "This task is no longer available.";
                return RedirectToAction("Index");
            }

            // Verify the task still exists and belongs to the manager's company
            bool taskExists = await _context.ProjectTasks.AnyAsync(t => t.ProjectTaskId == notification.ProjectTaskId.Value && t.Project!.CompanyId == manager.CompanyID.Value);

            // Prevent access if the task does not exist or belongs to another company
            if (!taskExists)
            {
                TempData["ErrorMessage"] = "This task is no longer available.";
                return RedirectToAction("Index");
            }

            // Mark the notification as read before opening the task
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;

                // Save the notification update to the database
                await _context.SaveChangesAsync();
            }

            // Redirect the manager to the related task details page
            return RedirectToAction("Details", "ManagerTask", new { id = notification.ProjectTaskId.Value });
        }
    }
}
