
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
    public class ManagerEmployeeControllerTests
    {
        // Create a separate in-memory database for each test
        private ApplicationDbContext GetDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // Create a fake UserManager for testing
        private Mock<UserManager<ApplicationUser>> GetUserManager()
        {
            // Create a fake user store
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

        // Create a controller with a simulated logged-in manager
        private ManagerEmployeeController GetController(
            ApplicationDbContext context,
            Mock<UserManager<ApplicationUser>> userManager,
            ApplicationUser? manager)
        {
            // Simulate the current manager
            userManager.Setup(u => u.GetUserAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(manager);

            // EmailService is not needed for these test scenarios
            var controller = new ManagerEmployeeController(
                context,
                userManager.Object,
                null!,
                Mock.Of<ILogger<ManagerEmployeeController>>()
            );

            // Create a fake HTTP context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        // Create a company for testing
        private async Task<Company> CreateCompany(
            ApplicationDbContext context, bool isActive = true)
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

        // Create a manager belonging to a specific company
        private ApplicationUser GetManager(int? companyId = 1, bool isActive = true)
        {
            return new ApplicationUser
            {
                Id = "manager1",
                FirstName = "John",
                LastName = "Smith",
                CompanyID = companyId,
                IsActive = isActive
            };
        }

        // Create an employee and save the employee profile
        private async Task<Employee> CreateEmployee(
            ApplicationDbContext context, int companyId, bool isActive = true)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "David",
                LastName = "Johnson",
                CompanyID = companyId,
                IsActive = isActive
            };

            var employee = new Employee
            {
                ApplicationUser = user,
                CompanyId = companyId,
                JobTitle = "Software Developer",
                Department = "IT",
                AddedDate = DateTime.Now,
                IsActive = isActive
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            return employee;
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenManagerIsNotLoggedIn()
        {
            // Arrange: Simulate a manager who is not logged in
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var controller = GetController(context, userManager, null);

            // Act: Try to view the employee list
            var result = await controller.Index(null, null, null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenManagerHasNoCompany()
        {
            // Arrange: Create a manager without a company
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var manager = GetManager(null);
            var controller = GetController(context, userManager, manager);

            // Act: Try to view company employees
            var result = await controller.Index(null, null, null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnEmptyList_WhenNoEmployeesExist()
        {
            // Arrange: Create a company without employees
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var userManager = GetUserManager();
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, userManager, manager);

            // Act: Request the employee list
            var result = await controller.Index(null, null, null);

            // Assert: Verify that the employee list is empty
            var view = Assert.IsType<ViewResult>(result);
            var employees = Assert.IsAssignableFrom<IEnumerable<Employee>>(view.Model);

            Assert.Empty(employees);
        }

        [Fact]
        public void Create_ShouldReturnView()
        {
            // Arrange: Create the controller
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var manager = GetManager();
            var controller = GetController(context, userManager, manager);

            // Act: Open the employee creation form
            var result = controller.Create();

            // Assert: Verify that an empty form is returned
            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<CreateEmployeeViewModel>(view.Model);
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenModelStateIsInvalid()
        {
            // Arrange: Create an invalid employee registration form
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var manager = GetManager();
            var controller = GetController(context, userManager, manager);

            var model = new CreateEmployeeViewModel
            {
                FirstName = "",
                LastName = "Johnson",
                Email = "david@example.com",
                JobTitle = "Developer",
                Department = "IT"
            };

            // Simulate a server-side validation error
            controller.ModelState.AddModelError(
                "FirstName", "First name is required.");

            // Act: Submit the invalid employee form
            var result = await controller.Create(model);

            // Assert: Verify that the form is returned
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);

            // Verify that no Identity account was created
            userManager.Verify(u => u.CreateAsync(
                It.IsAny<ApplicationUser>()), Times.Never());
        }

        [Fact]
        public async Task Edit_ShouldReturnNotFound_WhenEmployeeBelongsToAnotherCompany()
        {
            // Arrange: Create two different companies
            using var context = GetDatabase();
            var company1 = await CreateCompany(context);
            var company2 = await CreateCompany(context);

            // Create an employee belonging to company2
            var employee = await CreateEmployee(context, company2.CompanyId);

            // Create a manager belonging to company1
            var userManager = GetUserManager();
            var manager = GetManager(company1.CompanyId);
            var controller = GetController(context, userManager, manager);

            // Act: Try to edit an employee from another company
            var result = await controller.Edit(employee.EmployeeId);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPost_ShouldReturnView_WhenModelStateIsInvalid()
        {
            // Arrange: Create an invalid employee edit form
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var manager = GetManager();
            var controller = GetController(context, userManager, manager);

            var model = new EditEmployeeViewModel
            {
                EmployeeId = 1,
                FirstName = "",
                LastName = "Johnson",
                JobTitle = "Developer",
                Department = "IT"
            };

            // Simulate a validation error
            controller.ModelState.AddModelError(
                "FirstName", "First name is required.");

            // Act: Submit the invalid edit form
            var result = await controller.Edit(model);

            // Assert: Verify that the form is returned
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenEmployeeBelongsToAnotherCompany()
        {
            // Arrange: Create two companies
            using var context = GetDatabase();
            var company1 = await CreateCompany(context);
            var company2 = await CreateCompany(context);

            // Create an employee in company2
            var employee = await CreateEmployee(context, company2.CompanyId);

            // Create a manager in company1
            var userManager = GetUserManager();
            var manager = GetManager(company1.CompanyId);
            var controller = GetController(context, userManager, manager);

            // Act: Try to view another company's employee
            var result = await controller.Details(employee.EmployeeId);

            // Assert: Verify that access to the employee is denied
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Deactivate_ShouldReturnNotFound_WhenEmployeeBelongsToAnotherCompany()
        {
            // Arrange: Create two separate companies
            using var context = GetDatabase();
            var company1 = await CreateCompany(context);
            var company2 = await CreateCompany(context);

            // Create an active employee in company2
            var employee = await CreateEmployee(context, company2.CompanyId);

            // Create a manager in company1
            var userManager = GetUserManager();
            var manager = GetManager(company1.CompanyId);
            var controller = GetController(context, userManager, manager);

            // Act: Try to deactivate an employee from another company
            var result = await controller.Deactivate(employee.EmployeeId);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);

            // Verify that the employee remains active
            Assert.True(employee.IsActive);
            Assert.True(employee.ApplicationUser.IsActive);
        }

        [Fact]
        public async Task Activate_ShouldReturnNotFound_WhenEmployeeBelongsToAnotherCompany()
        {
            // Arrange: Create two separate companies
            using var context = GetDatabase();
            var company1 = await CreateCompany(context);
            var company2 = await CreateCompany(context);

            // Create an inactive employee in company2
            var employee = await CreateEmployee(context, company2.CompanyId, false);

            // Create a manager in company1
            var userManager = GetUserManager();
            var manager = GetManager(company1.CompanyId);
            var controller = GetController(context, userManager, manager);

            // Act: Try to activate an employee from another company
            var result = await controller.Activate(employee.EmployeeId);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);

            // Verify that the employee remains inactive
            Assert.False(employee.IsActive);
            Assert.False(employee.ApplicationUser.IsActive);
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnForbid_WhenManagerIsInactive()
        {
            // Arrange: Create an inactive manager
            using var context = GetDatabase();
            var userManager = GetUserManager();
            var manager = GetManager(1, false);
            var controller = GetController(context, userManager, manager);

            // Act: Try to open an employee password reset form
            var result = await controller.ResetPassword(1);

            // Assert: Verify that inactive managers are blocked
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ResetPasswordConfirmed_ShouldReturnForbid_WhenCompanyIsInactive()
        {
            // Arrange: Create an inactive company
            using var context = GetDatabase();
            var company = await CreateCompany(context, false);

            // Create a manager belonging to the inactive company
            var userManager = GetUserManager();
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, userManager, manager);

            // Act: Try to reset an employee password
            var result = await controller.ResetPasswordConfirmed(1);

            // Assert: Verify that inactive companies cannot reset passwords
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Activate_ShouldActivateEmployee()
        {
            // Arrange: Create an active company
            using var context = GetDatabase();
            var company = await CreateCompany(context);

            // Create an inactive employee
            var employee = await CreateEmployee(context, company.CompanyId, false);

            // Create the manager
            var userManager = GetUserManager();
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, userManager, manager);

            // Act: Activate the employee
            var result = await controller.Activate(employee.EmployeeId);

            // Assert: Verify that the controller redirects to Details
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);

            // Verify that both the employee and account are active
            Assert.True(employee.IsActive);
            Assert.True(employee.ApplicationUser.IsActive);

            // Verify that the account is unlocked
            Assert.Null(employee.ApplicationUser.LockoutEnd);
            Assert.Equal(0, employee.ApplicationUser.AccessFailedCount);
        }

        [Fact]
        public async Task Deactivate_ShouldDeactivateEmployee()
        {
            // Arrange: Create an active company
            using var context = GetDatabase();
            var company = await CreateCompany(context);

            // Create an active employee
            var employee = await CreateEmployee(context, company.CompanyId);

            // Create the manager
            var userManager = GetUserManager();
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, userManager, manager);

            // Simulate successful invalidation of existing login sessions
            userManager.Setup(u => u.UpdateSecurityStampAsync(
                It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act: Deactivate the employee
            var result = await controller.Deactivate(employee.EmployeeId);

            // Assert: Verify that the controller redirects to Details
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);

            // Verify that the employee and account are inactive
            Assert.False(employee.IsActive);
            Assert.False(employee.ApplicationUser.IsActive);

            // Verify that login is blocked
            Assert.True(employee.ApplicationUser.LockoutEnabled);
            Assert.Equal(DateTimeOffset.MaxValue, employee.ApplicationUser.LockoutEnd);

            // Verify that existing sessions are invalidated
            userManager.Verify(u => u.UpdateSecurityStampAsync(
                It.IsAny<ApplicationUser>()), Times.Once());
        }
    }
}
