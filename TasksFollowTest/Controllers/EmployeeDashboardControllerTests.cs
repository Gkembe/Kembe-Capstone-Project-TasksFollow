
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
    public class EmployeeDashboardControllerTests
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

            // Create UserManager using mocked dependencies
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

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenUserIsNotLoggedIn()
        {
            // Arrange: Create an empty database and a fake UserManager
            using var context = GetDatabase();
            var userManager = GetUserManager();

            // Simulate a user who is not logged in
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);

            var controller = new EmployeeDashboardController(context, userManager.Object);

            // Act: Try to access the employee dashboard
            var result = await controller.Index();

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenEmployeeHasNoCompany()
        {
            // Arrange: Create an employee without a company
            using var context = GetDatabase();
            var userManager = GetUserManager();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = null
            };

            // Simulate the logged-in employee
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var controller = new EmployeeDashboardController(context, userManager.Object);

            // Act: Try to access the dashboard without a company
            var result = await controller.Index();

            // Assert: Verify that the employee cannot access the dashboard
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenCompanyDoesNotExist()
        {
            // Arrange: Create an employee with a company ID that does not exist
            using var context = GetDatabase();
            var userManager = GetUserManager();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 999
            };

            // Simulate the logged-in employee
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var controller = new EmployeeDashboardController(context, userManager.Object);

            // Act: Try to access the dashboard using an invalid company ID
            var result = await controller.Index();

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenCompanyIsInactive()
        {
            // Arrange: Create an inactive company
            using var context = GetDatabase();
            var userManager = GetUserManager();

            var company = new Company
            {
                Name = "Company A",
                Address = "123 Main Street",
                IsActive = false
            };

            // Save the inactive company to the test database
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            // Create an employee belonging to the inactive company
            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = company.CompanyId
            };

            // Simulate the logged-in employee
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var controller = new EmployeeDashboardController(context, userManager.Object);

            // Act: Try to access the inactive company's dashboard
            var result = await controller.Index();

            // Assert: Verify that employees from inactive companies are blocked
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnDashboard_WhenCompanyIsActive()
        {
            // Arrange: Create an active company
            using var context = GetDatabase();
            var userManager = GetUserManager();

            var company = new Company
            {
                Name = "TasksFollow Company",
                Address = "123 Main Street",
                IsActive = true
            };

            // Save the active company to the test database
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            // Create an employee belonging to the active company
            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = company.CompanyId
            };

            // Simulate the logged-in employee
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var controller = new EmployeeDashboardController(context, userManager.Object);

            // Act: Open the employee dashboard
            var result = await controller.Index();

            // Assert: Verify that the controller returns the dashboard view
            var view = Assert.IsType<ViewResult>(result);

            // Get the dashboard information returned by the controller
            var model = Assert.IsType<EmployeeDashboardViewModel>(view.Model);

            // Verify that the correct company name is displayed
            Assert.Equal("TasksFollow Company", model.CompanyName);

            // Verify that all task counters are zero because no tasks exist
            Assert.Equal(0, model.TotalAssignedTasks);
            Assert.Equal(0, model.TotalCompletedTasks);
            Assert.Equal(0, model.TotalInProgressTasks);
            Assert.Equal(0, model.OverDueTasks);
        }
    }
}
