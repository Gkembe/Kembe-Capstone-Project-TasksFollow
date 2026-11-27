
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TasksFollow.Controllers;
using TasksFollow.Data;
using TasksFollow.Models;
using Xunit;

namespace TasksFollowTest.Controllers
{
    public class EmployeeNotificationControllerTests
    {
        // Create a separate in-memory database for each test
        private ApplicationDbContext GetDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // Create a fake UserManager to simulate logged-in employees
        private Mock<UserManager<ApplicationUser>> GetUserManager()
        {
            // Create a fake user store required by UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();

            // Create UserManager with mocked dependencies
            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                new List<IUserValidator<ApplicationUser>>(),
                new List<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );
        }

        // Create a controller with a simulated logged-in employee
        private EmployeeNotificationController GetController(
            ApplicationDbContext context,
            ApplicationUser user)
        {
            var userManager = GetUserManager();

            // Return the employee when the controller requests the current user
            userManager.Setup(u => u.GetUserAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var controller = new EmployeeNotificationController(context, userManager.Object);

            // Create a fake HTTP context for the controller
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Configure TempData for success and error messages
            controller.TempData = new TempDataDictionary(
                controller.HttpContext,
                Mock.Of<ITempDataProvider>()
            );

            return controller;
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenUserIsNotLoggedIn()
        {
            // Arrange: Simulate a user who is not logged in
            using var context = GetDatabase();
            var controller = GetController(context, null);

            // Act: Try to open the notifications page
            var result = await controller.Index();

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenEmployeeIsInactive()
        {
            // Arrange: Create an inactive employee
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 1,
                IsActive = false
            };

            var controller = GetController(context, user);

            // Act: Try to open the notifications page
            var result = await controller.Index();

            // Assert: Verify that inactive employees are blocked
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenEmployeeHasNoCompany()
        {
            // Arrange: Create an employee without a company
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = null,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Try to open the notifications page
            var result = await controller.Index();

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnOnlyEmployeeNotifications()
        {
            // Arrange: Create notifications for two different employees
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 1,
                IsActive = true
            };

            context.Notifications.Add(new Notification
            {
                UserId = "employee1",
                Title = "Task Assigned",
                Message = "You have a new task.",
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            context.Notifications.Add(new Notification
            {
                UserId = "employee2",
                Title = "Task Completed",
                Message = "Your task was completed.",
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            await context.SaveChangesAsync();

            var controller = GetController(context, user);

            // Act: Get notifications for employee1
            var result = await controller.Index();

            // Assert: Verify that only employee1's notification is returned
            var view = Assert.IsType<ViewResult>(result);
            var notifications = Assert.IsAssignableFrom<IEnumerable<Notification>>(view.Model);

            Assert.Single(notifications);
            Assert.Equal("employee1", notifications.First().UserId);
        }

        [Fact]
        public async Task MarkAsRead_ShouldMarkNotificationAsRead()
        {
            // Arrange: Create an unread notification
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 1,
                IsActive = true
            };

            var notification = new Notification
            {
                UserId = "employee1",
                Title = "Task Assigned",
                Message = "You have a new task.",
                IsRead = false,
                CreatedDate = DateTime.Now
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var controller = GetController(context, user);

            // Act: Mark the notification as read
            var result = await controller.MarkAsRead(notification.NotificationId);

            // Assert: Verify that the notification is marked as read
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);
            Assert.True(notification.IsRead);
            Assert.NotNull(notification.ReadDate);
        }

        [Fact]
        public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationDoesNotExist()
        {
            // Arrange: Create an employee without notifications
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 1,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Try to mark a nonexistent notification as read
            var result = await controller.MarkAsRead(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationBelongsToAnotherEmployee()
        {
            // Arrange: Create a notification belonging to employee2
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 1,
                IsActive = true
            };

            var notification = new Notification
            {
                UserId = "employee2",
                Title = "Task Assigned",
                Message = "You have a new task.",
                IsRead = false,
                CreatedDate = DateTime.Now
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var controller = GetController(context, user);

            // Act: Employee1 tries to read employee2's notification
            var result = await controller.MarkAsRead(notification.NotificationId);

            // Assert: Verify that access to another employee's notification is denied
            Assert.IsType<NotFoundResult>(result);
            Assert.False(notification.IsRead);
        }

        [Fact]
        public async Task MarkAllAsRead_ShouldMarkOnlyEmployeeNotificationsAsRead()
        {
            // Arrange: Create unread notifications for two employees
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 1,
                IsActive = true
            };

            var notification1 = new Notification
            {
                UserId = "employee1",
                Title = "Task Assigned",
                Message = "New task assigned.",
                IsRead = false,
                CreatedDate = DateTime.Now
            };

            var notification2 = new Notification
            {
                UserId = "employee1",
                Title = "Task Updated",
                Message = "Task information updated.",
                IsRead = false,
                CreatedDate = DateTime.Now
            };

            var notification3 = new Notification
            {
                UserId = "employee2",
                Title = "Task Assigned",
                Message = "Another employee's task.",
                IsRead = false,
                CreatedDate = DateTime.Now
            };

            context.Notifications.AddRange(notification1, notification2, notification3);
            await context.SaveChangesAsync();

            var controller = GetController(context, user);

            // Act: Mark all notifications for employee1 as read
            var result = await controller.MarkAllAsRead();

            // Assert: Verify that only employee1's notifications were updated
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);
            Assert.True(notification1.IsRead);
            Assert.True(notification2.IsRead);
            Assert.False(notification3.IsRead);
            Assert.NotNull(notification1.ReadDate);
            Assert.NotNull(notification2.ReadDate);
            Assert.Equal("All notifications marked as read.", controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task OpenTask_ShouldReturnNotFound_WhenNotificationDoesNotExist()
        {
            // Arrange: Create an employee without notifications
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 1,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Try to open a nonexistent notification
            var result = await controller.OpenTask(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OpenTask_ShouldRedirectToIndex_WhenNotificationIsNotTaskAssignment()
        {
            // Arrange: Create a notification that is not linked to a task
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 1,
                IsActive = true
            };

            var notification = new Notification
            {
                UserId = "employee1",
                Title = "General Notification",
                Message = "Welcome to TasksFollow.",
                IsRead = false,
                CreatedDate = DateTime.Now,
                ProjectTaskId = null
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var controller = GetController(context, user);

            // Act: Try to open a notification without a task assignment
            var result = await controller.OpenTask(notification.NotificationId);

            // Assert: Verify that the employee returns to the notification list
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);
            Assert.True(notification.IsRead);
            Assert.NotNull(notification.ReadDate);
        }
    }
}
