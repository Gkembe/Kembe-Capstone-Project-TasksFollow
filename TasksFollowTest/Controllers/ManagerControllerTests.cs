
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
    public class ManagerControllerTests
    {
        // Create a separate in-memory database for each test
        private ApplicationDbContext GetDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // Create a fake UserManager for testing manager accounts
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

        // Create a ManagerController for tests that do not send emails
        private ManagerController GetController(
            ApplicationDbContext context,
            Mock<UserManager<ApplicationUser>> userManager)
        {
            // EmailService is not used by the test scenarios below
            var controller = new ManagerController(
                context,
                userManager.Object,
                null!,
                Mock.Of<ILogger<ManagerController>>()
            );

            return controller;
        }

        // Create a company for testing
        private async Task<Company> CreateCompany(ApplicationDbContext context)
        {
            var company = new Company
            {
                Name = "TasksFollow Company",
                Address = "123 Main Street",
                IsActive = true
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            return company;
        }

        [Fact]
        public async Task Index_ShouldReturnEmptyList_WhenNoManagersExist()
        {
            // Arrange: Create an empty manager list
            using var context = GetDatabase();
            var userManager = GetUserManager();

            userManager.Setup(u => u.GetUsersInRoleAsync("Manager"))
                .ReturnsAsync(new List<ApplicationUser>());

            var controller = GetController(context, userManager);

            // Act: Request the manager list
            var result = await controller.Index(null);

            // Assert: Verify that an empty list is returned
            var view = Assert.IsType<ViewResult>(result);
            var managers = Assert.IsAssignableFrom<IEnumerable<ManagerIndexViewModel>>(view.Model);

            Assert.Empty(managers);
        }

        [Fact]
        public async Task Create_ShouldReturnNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var controller = GetController(context, userManager);

            // Act: Try to open the creation form for an unknown company
            var result = await controller.Create(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ShouldReturnView_WhenCompanyExists()
        {
            // Arrange: Create an active company
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var userManager = GetUserManager();
            var controller = GetController(context, userManager);

            // Act: Open the manager creation form
            var result = await controller.Create(company.CompanyId);

            // Assert: Verify that the correct company is selected
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Manager>(view.Model);

            Assert.Equal(company.CompanyId, model.CompanyId);
            Assert.Equal("TasksFollow Company", controller.ViewBag.CompanyName);
        }

        [Fact]
        public async Task CreatePost_ShouldReturnNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange: Create a manager for a nonexistent company
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var controller = GetController(context, userManager);

            var manager = new Manager
            {
                CompanyId = 999,
                FirstName = "John",
                LastName = "Smith",
                Email = "john@example.com"
            };

            // Act: Submit the manager creation form
            var result = await controller.Create(manager);

            // Assert: Verify that the company must exist
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenModelStateIsInvalid()
        {
            // Arrange: Create a company and an invalid manager form
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var userManager = GetUserManager();
            var controller = GetController(context, userManager);

            var manager = new Manager
            {
                CompanyId = company.CompanyId,
                FirstName = "",
                LastName = "Smith",
                Email = "john@example.com"
            };

            // Simulate a server-side validation error
            controller.ModelState.AddModelError("FirstName", "First name is required.");

            // Act: Submit the invalid form
            var result = await controller.Create(manager);

            // Assert: Verify that the form is returned without creating a manager
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(manager, view.Model);
            Assert.False(controller.ModelState.IsValid);

            // Verify that Identity was not asked to create an account
            userManager.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never());
        }

        [Fact]
        public async Task Edit_ShouldReturnNotFound_WhenIdIsNull()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var controller = GetController(context, userManager);

            // Act: Try to edit a manager without an ID
            var result = await controller.Edit((string)null!);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_ShouldReturnNotFound_WhenManagerDoesNotExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var controller = GetController(context, userManager);

            // Act: Try to edit a nonexistent manager
            var result = await controller.Edit("manager999");

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPost_ShouldReturnView_WhenModelStateIsInvalid()
        {
            // Arrange: Create an invalid manager edit form
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var controller = GetController(context, userManager);

            var manager = new ManagerEditViewModel
            {
                Id = "manager1",
                FirstName = "",
                LastName = "Smith",
                Email = "john@example.com"
            };

            // Simulate a validation error
            controller.ModelState.AddModelError("FirstName", "First name is required.");

            // Act: Submit the invalid edit form
            var result = await controller.Edit(manager);

            // Assert: Verify that the form is returned without updating the user
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(manager, view.Model);
            Assert.False(controller.ModelState.IsValid);

            userManager.Verify(u => u.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never());
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenManagerDoesNotExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var controller = GetController(context, userManager);

            // Act: Request details for a nonexistent manager
            var result = await controller.Details("manager999");

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Activate_ShouldReturnNotFound_WhenManagerDoesNotExist()
        {
            // Arrange: Simulate a manager that does not exist
            using var context = GetDatabase();
            var userManager = GetUserManager();

            userManager.Setup(u => u.FindByIdAsync("manager999"))
                .ReturnsAsync((ApplicationUser?)null);

            var controller = GetController(context, userManager);

            // Act: Try to activate the nonexistent manager
            var result = await controller.Activate("manager999");

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Deactivate_ShouldReturnNotFound_WhenManagerDoesNotExist()
        {
            // Arrange: Simulate a manager that does not exist
            using var context = GetDatabase();
            var userManager = GetUserManager();

            userManager.Setup(u => u.FindByIdAsync("manager999"))
                .ReturnsAsync((ApplicationUser?)null);

            var controller = GetController(context, userManager);

            // Act: Try to deactivate the nonexistent manager
            var result = await controller.Deactivate("manager999");

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnNotFound_WhenManagerDoesNotExist()
        {
            // Arrange: Simulate a manager that does not exist
            using var context = GetDatabase();
            var userManager = GetUserManager();

            userManager.Setup(u => u.FindByIdAsync("manager999"))
                .ReturnsAsync((ApplicationUser?)null);

            var controller = GetController(context, userManager);

            // Act: Try to open the password reset form
            var result = await controller.ResetPassword("manager999");

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ResetPasswordPost_ShouldReturnNotFound_WhenManagerDoesNotExist()
        {
            // Arrange: Create a password reset request for a nonexistent manager
            using var context = GetDatabase();
            var userManager = GetUserManager();

            userManager.Setup(u => u.FindByIdAsync("manager999"))
                .ReturnsAsync((ApplicationUser?)null);

            var controller = GetController(context, userManager);

            var model = new AdminResetPasswordViewModel
            {
                ManagerId = "manager999"
            };

            // Act: Submit the password reset request
            var result = await controller.ResetPassword(model);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
