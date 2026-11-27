
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
    public class EmployeeProjectControllerTests
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
        private EmployeeProjectController GetController(ApplicationDbContext context, ApplicationUser user)
        {
            var userManager = GetUserManager();

            // Return the employee when the controller requests the current user
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var controller = new EmployeeProjectController(context, userManager.Object);

            // Create a fake HTTP request for checking query parameters
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        // Create and save a company for testing
        private async Task<Company> CreateCompany(ApplicationDbContext context, bool isActive = true)
        {
            var company = new Company
            {
                Name = "TasksFollow Company",
                Address = "123 Main Street",
                IsActive = isActive
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            return company;
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenUserIsNotLoggedIn()
        {
            // Arrange: Simulate a user who is not logged in
            using var context = GetDatabase();
            var controller = GetController(context, null);

            // Act: Try to open the employee projects page
            var result = await controller.Index(null, null);

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

            // Act: Try to open the projects page
            var result = await controller.Index(null, null);

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

            // Act: Try to open the projects page
            var result = await controller.Index(null, null);

            // Assert: Verify that employees without a company are blocked
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenCompanyIsInactive()
        {
            // Arrange: Create an inactive company
            using var context = GetDatabase();
            var company = await CreateCompany(context, false);

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = company.CompanyId,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Try to open projects belonging to an inactive company
            var result = await controller.Index(null, null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenCompanyDoesNotExist()
        {
            // Arrange: Create an employee with an invalid company ID
            using var context = GetDatabase();

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = 999,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Try to open the projects page
            var result = await controller.Index(null, null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnEmptyList_WhenEmployeeHasNoProjects()
        {
            // Arrange: Create an employee belonging to an active company
            using var context = GetDatabase();
            var company = await CreateCompany(context);

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = company.CompanyId,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Get projects for an employee without project assignments
            var result = await controller.Index(null, null);

            // Assert: Verify that the project list is empty
            var view = Assert.IsType<ViewResult>(result);
            var projects = Assert.IsAssignableFrom<IEnumerable<Project>>(view.Model);

            Assert.Empty(projects);
        }

        [Fact]
        public async Task Index_ShouldReturnEmptyList_WhenSearchHasNoMatches()
        {
            // Arrange: Create an active company and employee
            using var context = GetDatabase();
            var company = await CreateCompany(context);

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = company.CompanyId,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Search for a project that does not exist
            var result = await controller.Index("Unknown Project", null);

            // Assert: Verify that no projects are returned
            var view = Assert.IsType<ViewResult>(result);
            var projects = Assert.IsAssignableFrom<IEnumerable<Project>>(view.Model);

            Assert.Empty(projects);
        }

        [Fact]
        public async Task Details_ShouldReturnForbid_WhenUserIsNotLoggedIn()
        {
            // Arrange: Simulate a user who is not logged in
            using var context = GetDatabase();
            var controller = GetController(context, null);

            // Act: Try to open project details
            var result = await controller.Details(1);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenProjectDoesNotExist()
        {
            // Arrange: Create an active company and employee
            using var context = GetDatabase();
            var company = await CreateCompany(context);

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = company.CompanyId,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Request details for a project that does not exist
            var result = await controller.Details(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenEmployeeIsNotProjectMember()
        {
            // Arrange: Create an active company and employee
            using var context = GetDatabase();
            var company = await CreateCompany(context);

            var user = new ApplicationUser
            {
                Id = "employee1",
                CompanyID = company.CompanyId,
                IsActive = true
            };

            var controller = GetController(context, user);

            // Act: Request project details without project membership
            var result = await controller.Details(999);

            // Assert: Verify that access to the project is denied
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
