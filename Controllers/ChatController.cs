using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
   
    [Authorize(Roles = "Manager,Employee")]
    public class ChatController : Controller
    {
        
        private readonly ApplicationDbContext _context;
      
        private readonly UserManager<ApplicationUser> _userManager;

      
        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Chat - List all groups the current user can access.
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            int companyId = user.CompanyID.Value;

            bool isManager = User.IsInRole("Manager");

            // Get active groups belonging to the user's company
            var groups = _context.GroupChats.Include(g => g.GroupMembers).Where(g => g.CompanyId == companyId && g.Company.IsActive && g.IsActive);

            // Employees can only see groups they belong to
            if (!isManager)
            {
                groups = groups.Where(g => g.GroupMembers.Any(m => m.Employee.ApplicationUserId == user.Id && m.Employee.CompanyId == companyId && m.Employee.IsActive && m.IsActive));
            }

            // Sort the groups by name before building the dashboard list.
            var groupList = await groups.OrderBy(g => g.Name).ToListAsync();

            var model = new List<ChatIndexViewModel>();

            foreach (var group in groupList)
            {
                // Count unread messages sent by other users
                int unread = await _context.Messages.CountAsync(m => m.GroupChatId == group.GroupChatId && !m.IsDeleted && m.UserId != user.Id && !_context.MessageReads.Any(r => r.MessageId == m.MessageId && r.UserId == user.Id));

                model.Add(new ChatIndexViewModel
                {
                    Group = group,
                    UnreadCount = unread
                });
            }

            return View(model);
        }



        // GET: Chat/Details/5 - Display group messages and mark incoming messages as read.
        public async Task<IActionResult> Details(int id)
        {
            // Get the currently logged-in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            int companyId = user.CompanyID.Value;
            bool isManager = User.IsInRole("Manager");

            // Find the active group belonging to the user's company
            var group = await _context.GroupChats.Include(g => g.GroupMembers).FirstOrDefaultAsync(g => g.GroupChatId == id && g.CompanyId == companyId && g.Company.IsActive && g.IsActive);

            if (group == null)
            {
                return NotFound();
            }

            // Employees must be active members of the group
            if (!isManager)
            {
                bool isMember = await _context.GroupMembers.AnyAsync(m => m.GroupChatId == id && m.Employee.ApplicationUserId == user.Id && m.Employee.CompanyId == companyId && m.Employee.IsActive && m.IsActive);

                if (!isMember)
                {
                    return Forbid();
                }
            }

            // Load only visible messages, ordered by date
            // AsNoTracking prevents EF Core from tracking these display-only records
            var messages = await _context.Messages.AsNoTracking().Include(m => m.User).Where(m => m.GroupChatId == id && !m.IsDeleted).OrderBy(m => m.SentDate).ThenBy(m => m.MessageId).ToListAsync();

            // Find messages the current user has not read
            var unreadMessages = await _context.Messages.Where(m => m.GroupChatId == id && !m.IsDeleted && m.UserId != user.Id && !_context.MessageReads.Any(r => r.MessageId == m.MessageId && r.UserId == user.Id)).Select(m => m.MessageId).ToListAsync();

            // Create a read receipt for each unread message from another user.
            foreach (int messageId in unreadMessages)
            {
                _context.MessageReads.Add(new MessageRead
                {
                    MessageId = messageId,
                    UserId = user.Id,
                    ReadDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            // Assign display-only messages without changing tracked relationships
            group.Messages = messages;

            return View(group);
        }

        // GET: Chat - List all groups the current user can access./Create
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create()
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            int companyId = manager.CompanyID.Value;

            // Verify company is active
            if (!await _context.Companies.AnyAsync(c => c.CompanyId == companyId && c.IsActive))
            {
                return Forbid();
            }

            // Load active employees who can be added to this group. from manager's company
            var employees = await _context.Employees.Include(e => e.ApplicationUser).Where(e => e.CompanyId == companyId && e.IsActive && e.ApplicationUser.IsActive).OrderBy(e => e.ApplicationUser.FirstName).ToListAsync();

            var model = new CreateGroupViewModel();

            foreach (var employee in employees)
            {
                model.Employees.Add(new SelectListItem
                {
                    Value = employee.EmployeeId.ToString(),

                    Text = employee.ApplicationUser.FirstName + " " + employee.ApplicationUser.LastName
                });
            }

            return View(model);
        }

        // POST: Chat/Create - Validate selections and save a new group with its members.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            int companyId = manager.CompanyID.Value;

            if (!await _context.Companies.AnyAsync(c => c.CompanyId == companyId && c.IsActive))
            {
                return Forbid();
            }

            // Load active employees in this company to validate submitted member IDs. from this company
            var employees = await _context.Employees.Include(e => e.ApplicationUser).Where(e => e.CompanyId == companyId && e.IsActive && e.ApplicationUser.IsActive).OrderBy(e => e.ApplicationUser.FirstName).ToListAsync();

            var selectedIds = model.SelectedEmployeeIds.Distinct().ToList();

            // Validate selected employees
            if (selectedIds.Count == 0)
            {
                ModelState.AddModelError("SelectedEmployeeIds", "Select at least one employee.");
            }

            if (employees.Count(e => selectedIds.Contains(e.EmployeeId)) != selectedIds.Count)
            {
                ModelState.AddModelError("SelectedEmployeeIds", "One or more selected employees are invalid.");
            }

            if (!ModelState.IsValid)
            {
                // Reload employee list when validation fails
                foreach (var employee in employees)
                {
                    model.Employees.Add(new SelectListItem
                    {
                        Value = employee.EmployeeId.ToString(),

                        Text = employee.ApplicationUser.FirstName + " " + employee.ApplicationUser.LastName
                    });
                }

                return View(model);
            }

            // Create group chat
            var group = new GroupChat
            {
                Name = model.Name.Trim(),
                Description = model.Description,
                CompanyId = companyId,
                CreatedByUserId = manager.Id,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            // Add selected employees to group
            foreach (int employeeId in selectedIds)
            {
                group.GroupMembers.Add(new GroupMember
                {
                    EmployeeId = employeeId,
                    JoinedDate = DateTime.Now,
                    IsActive = true
                });
            }

            // Save the new group and the related member records in one operation.
            _context.GroupChats.Add(group);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Group created successfully.";

            return RedirectToAction("Details", new { id = group.GroupChatId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // POST: Chat/SendMessage - Add a new message to an accessible group.
        public async Task<IActionResult> SendMessage(int groupChatId, string content)
        {
            // Get logged-in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            int companyId = user.CompanyID.Value;

            bool isManager = User.IsInRole("Manager");

            // Reject messages to groups outside the user's active company.
            var group = await _context.GroupChats.FirstOrDefaultAsync(g => g.GroupChatId == groupChatId && g.CompanyId == companyId && g.Company.IsActive && g.IsActive);

            if (group == null)
            {
                return NotFound();
            }

            // Employees must be active group members before they can send messages.
            if (!isManager)
            {
                bool isMember = await _context.GroupMembers.AnyAsync(m => m.GroupChatId == groupChatId && m.Employee.ApplicationUserId == user.Id && m.Employee.CompanyId == companyId && m.Employee.IsActive && m.IsActive);

                if (!isMember)
                {
                    return Forbid();
                }
            }

            // Prevent blank messages and enforce the 3,000-character limit.
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";

                return RedirectToAction("Details", new { id = groupChatId });
            }

            content = content.Trim();

            if (content.Length > 3000)
            {
                TempData["ErrorMessage"] = "Message cannot exceed 3000 characters.";
                return RedirectToAction("Details", new { id = groupChatId });
            }

            // Build the new message record with its sender and group.
            var message = new Message
            {
                GroupChatId = groupChatId,
                UserId = user.Id,
                Content = content,
                SentDate = DateTime.Now,
                IsDeleted = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message sent successfully.";

            return RedirectToAction("Details", new { id = groupChatId });
        }

        [HttpGet]
        [Authorize(Roles = "Manager")]
        // GET: Chat/ManageMembers - Show current members and available employees.
        public async Task<IActionResult> ManageMembers(int id)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            int companyId = manager.CompanyID.Value;

            // Get group from manager's company
            var group = await _context.GroupChats.Include(g => g.GroupMembers).FirstOrDefaultAsync(g => g.GroupChatId == id && g.CompanyId == companyId && g.Company.IsActive && g.IsActive);

            if (group == null)
            {
                return NotFound();
            }

            // Load active employees who can be added to this group.
            var employees = await _context.Employees.Include(e => e.ApplicationUser).Where(e => e.CompanyId == companyId && e.IsActive && e.ApplicationUser.IsActive).OrderBy(e => e.ApplicationUser.FirstName).ToListAsync();

            var model = new ManageGroupMembersViewModel
            {
                GroupChatId = group.GroupChatId,
                GroupName = group.Name,
                SelectedEmployeeIds = group.GroupMembers.Where(m => m.IsActive).Select(m => m.EmployeeId).ToList()
            };

            foreach (var employee in employees)
            {
                model.Employees.Add(new SelectListItem
                {
                    Value = employee.EmployeeId.ToString(),
                    Text = employee.ApplicationUser.FirstName + " " + employee.ApplicationUser.LastName
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        // POST: Chat/ManageMembers - Update group membership without deleting history.
        public async Task<IActionResult> ManageMembers(ManageGroupMembersViewModel model)
        {
            var manager = await _userManager.GetUserAsync(User);

            if (manager == null || manager.CompanyID == null || !manager.IsActive)
            {
                return Forbid();
            }

            int companyId = manager.CompanyID.Value;

            // Get group and existing members
            var group = await _context.GroupChats.Include(g => g.GroupMembers).FirstOrDefaultAsync(g => g.GroupChatId == model.GroupChatId && g.CompanyId == companyId && g.Company.IsActive && g.IsActive);

            if (group == null)
            {
                return NotFound();
            }

            // Load active employees in this company to validate submitted member IDs.
            var employees = await _context.Employees.Include(e => e.ApplicationUser).Where(e => e.CompanyId == companyId && e.IsActive && e.ApplicationUser.IsActive).ToListAsync();

            var selectedIds = model.SelectedEmployeeIds.Distinct().ToList();

            // Validate selected employees
            if (employees.Count(e => selectedIds.Contains(e.EmployeeId)) != selectedIds.Count)
            {
                ModelState.AddModelError("SelectedEmployeeIds", "Invalid employee selection.");
            }

            if (!ModelState.IsValid)
            {
                model.GroupName = group.Name;

                foreach (var employee in employees)
                {
                    model.Employees.Add(new SelectListItem
                    {
                        Value = employee.EmployeeId.ToString(),
                        Text = employee.ApplicationUser.FirstName + " " + employee.ApplicationUser.LastName
                    });
                }

                return View(model);
            }

            // Activate selected existing members and deactivate unselected ones.
            foreach (var member in group.GroupMembers)
            {
                member.IsActive = selectedIds.Contains(member.EmployeeId);
            }

            // Add membership records for selected employees who were not previously members.
            foreach (int employeeId in selectedIds)
            {
                if (!group.GroupMembers.Any(m => m.EmployeeId == employeeId))
                {
                    group.GroupMembers.Add(new GroupMember
                    {
                        EmployeeId = employeeId,
                        JoinedDate = DateTime.Now,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Group members updated successfully.";

            return RedirectToAction("Details", new { id = group.GroupChatId });
        }

        [HttpGet]
        // GET: Chat/EditMessage - Show the form for editing the user's own message.
        public async Task<IActionResult> EditMessage(int id)
        {
            // Get logged-in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Get message from user's company
            var message = await _context.Messages.Include(m => m.GroupChat).FirstOrDefaultAsync(m => m.MessageId == id && m.UserId == user.Id && !m.IsDeleted && m.GroupChat.CompanyId == user.CompanyID.Value && m.GroupChat.Company.IsActive && m.GroupChat.IsActive);

            if (message == null)
            {
                return NotFound();
            }

            // Managers have company-level access; employees must belong to this group.
            if (!User.IsInRole("Manager"))
            {
                bool isMember = await _context.GroupMembers.AnyAsync(m => m.GroupChatId == message.GroupChatId && m.Employee.ApplicationUserId == user.Id && m.Employee.CompanyId == user.CompanyID.Value && m.Employee.IsActive && m.IsActive);

                if (!isMember)
                {
                    return Forbid();
                }
            }

            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // POST: Chat/EditMessage - Validate and save edits to the user's own message.
        public async Task<IActionResult> EditMessage(int id, string content)
        {
            // Get logged-in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Only retrieve messages authored by this user in an active group in their company.
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == id && m.UserId == user.Id && !m.IsDeleted && m.GroupChat.CompanyId == user.CompanyID.Value && m.GroupChat.Company.IsActive && m.GroupChat.IsActive);

            if (message == null)
            {
                return NotFound();
            }

            // Managers have company-level access; employees must belong to this group.
            if (!User.IsInRole("Manager"))
            {
                bool isMember = await _context.GroupMembers.AnyAsync(m => m.GroupChatId == message.GroupChatId && m.Employee.ApplicationUserId == user.Id && m.Employee.CompanyId == user.CompanyID.Value && m.Employee.IsActive && m.IsActive);

                if (!isMember)
                {
                    return Forbid();
                }
            }

            // Reject empty messages and messages longer than 3,000 characters.
            if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > 3000)
            {
                ModelState.AddModelError("Content", "Message must contain between 1 and 3000 characters.");

                message.Content = content;

                return View(message);
            }

            // Save the edited content and record when the message was edited.
            message.Content = content.Trim();
            message.EditedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message updated successfully.";

            return RedirectToAction("Details", new { id = message.GroupChatId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // POST: Chat/DeleteMessage - Soft-delete a message authored by this user.
        public async Task<IActionResult> DeleteMessage(int id)
        {
            // Get logged-in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Only retrieve messages authored by this user in an active group in their company.
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == id && m.UserId == user.Id && !m.IsDeleted && m.GroupChat.CompanyId == user.CompanyID.Value && m.GroupChat.Company.IsActive && m.GroupChat.IsActive);

            if (message == null)
            {
                return NotFound();
            }

            // Managers have company-level access; employees must belong to this group.
            if (!User.IsInRole("Manager"))
            {
                bool isMember = await _context.GroupMembers.AnyAsync(m => m.GroupChatId == message.GroupChatId && m.Employee.ApplicationUserId == user.Id && m.Employee.CompanyId == user.CompanyID.Value && m.Employee.IsActive && m.IsActive);

                if (!isMember)
                {
                    return Forbid();
                }
            }

            // Soft-delete the message so it no longer appears in the chat history.
            message.IsDeleted = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message deleted successfully.";

            return RedirectToAction("Details", new { id = message.GroupChatId });
        }




        [HttpGet]
        [Authorize(Roles = "Manager")]
        // GET: Chat/Delete - Show a confirmation page for deleting a group.
        public async Task<IActionResult> Delete(int id)
        {
            // Get the logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Find the active group belonging to this manager's company
            var group = await _context.GroupChats.FirstOrDefaultAsync(g => g.GroupChatId == id && g.CompanyId == user.CompanyID.Value && g.Company.IsActive && g.IsActive);

            if (group == null)
            {
                return NotFound();
            }

            // Display the delete confirmation page
            return View(group);
        }





        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        // POST: Chat/Delete - Deactivate a group after confirmation.
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Get the logged-in manager
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.CompanyID == null || !user.IsActive)
            {
                return Forbid();
            }

            // Find the group in the manager's company
            var group = await _context.GroupChats.FirstOrDefaultAsync(g => g.GroupChatId == id && g.CompanyId == user.CompanyID.Value && g.Company.IsActive && g.IsActive);

            if (group == null)
            {
                return NotFound();
            }

            // Deactivate the group without permanently deleting its messages or memberships.
            group.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Group deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

    }
}
