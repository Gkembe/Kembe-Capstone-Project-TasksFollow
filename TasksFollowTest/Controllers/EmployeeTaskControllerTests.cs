
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    public class EmployeeTaskControllerTests
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

            // Create a UserManager using mocked dependencies
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
        private EmployeeTaskController GetController(ApplicationDbContext context, ApplicationUser? user)
        {
            var userManager = GetUserManager();

            // Return the employee when the controller requests the current user
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var controller = new EmployeeTaskController(context, userManager.Object);

            // Create a fake HTTP context for accessing request information
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        // Create a valid employee for testing
        private ApplicationUser GetEmployee(int? companyId = 1)
        {
            return new ApplicationUser
            {
                Id = "employee1",
                FirstName = "John",
                LastName = "Smith",
                CompanyID = companyId,
                IsActive = true
            };
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenUserIsNotLoggedIn()
        {
            // Arrange: Simulate a user who is not logged in
            using var context = GetDatabase();
            var controller = GetController(context, null);

            // Act: Try to open the employee task list
            var result = await controller.Index(null, null, null, null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenCompanyIsInactive()
        {
            // Arrange: Create an inactive company
            using var context = GetDatabase();

            var company = new Company
            {
                Name = "TasksFollow Company",
                Address = "123 Main Street",
                IsActive = false
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            // Create an employee belonging to the inactive company
            var user = GetEmployee(company.CompanyId);
            var controller = GetController(context, user);

            // Act: Try to open the task list
            var result = await controller.Index(null, null, null, null);

            // Assert: Verify that employees cannot access tasks from inactive companies
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
        {
            // Arrange: Create an employee without task assignments
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Request an assignment that does not exist
            var result = await controller.Details(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Complete_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
        {
            // Arrange: Create an employee without task assignments
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Try to complete an assignment that does not exist
            var result = await controller.Complete(999);

            // Assert: Verify that the assignment cannot be completed
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Reopen_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
        {
            // Arrange: Create an employee without completed assignments
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Try to reopen an assignment that does not exist
            var result = await controller.Reopen(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateProgress_ShouldReturnBadRequest_WhenPercentIsNegative()
        {
            // Arrange: Create an active employee
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Try to update task progress to -10%
            var result = await controller.UpdateProgress(1, -10);

            // Assert: Verify that negative progress is rejected
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateProgress_ShouldReturnBadRequest_WhenPercentExceeds100()
        {
            // Arrange: Create an active employee
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Try to update task progress to 150%
            var result = await controller.UpdateProgress(1, 150);

            // Assert: Verify that progress above 100% is rejected
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateProgress_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
        {
            // Arrange: Create an employee without task assignments
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Try to update a nonexistent assignment
            var result = await controller.UpdateProgress(999, 50);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AllTasks_ShouldReturnForbid_WhenEmployeeHasNoCompany()
        {
            // Arrange: Create an employee without a company
            using var context = GetDatabase();
            var user = GetEmployee(null);
            var controller = GetController(context, user);

            // Act: Try to view all company tasks
            var result = await controller.AllTasks(null, null, null);

            // Assert: Verify that employees without companies are blocked
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Comments_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            // Arrange: Create an employee without existing tasks
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Try to view comments for a nonexistent task
            var result = await controller.Comments(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ReplyComment_ShouldReturnNotFound_WhenCommentDoesNotExist()
        {
            // Arrange: Create an employee without existing comments
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Try to reply to a nonexistent comment
            var result = await controller.ReplyComment(999, "Test reply");

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteComment_ShouldReturnNotFound_WhenCommentDoesNotExist()
        {
            // Arrange: Create an employee without existing comments
            using var context = GetDatabase();
            var user = GetEmployee();
            var controller = GetController(context, user);

            // Act: Try to delete a nonexistent comment
            var result = await controller.DeleteComment(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
