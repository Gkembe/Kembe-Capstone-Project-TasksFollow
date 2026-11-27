
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
    public class ManagerTaskControllerTests
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

        // Create a controller with a simulated logged-in manager
        private ManagerTaskController GetController(ApplicationDbContext context, ApplicationUser? manager)
        {
            var userManager = GetUserManager();

            // Simulate the currently logged-in manager
            userManager.Setup(u => u.GetUserAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(manager);

            // EmailService is not used in these test scenarios
            var controller = new ManagerTaskController(context, userManager.Object, null!);

            // Create a fake HTTP context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Configure TempData for controller messages
            controller.TempData = new TempDataDictionary(
                controller.HttpContext,
                Mock.Of<ITempDataProvider>()
            );

            return controller;
        }

        // Create and save a test company
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

        [Fact]
        public async Task Create_ShouldReturnForbid_WhenManagerIsNotLoggedIn()
        {
            // Arrange: Simulate a manager who is not logged in
            using var context = GetDatabase();
            var controller = GetController(context, null);

            // Act: Try to open the task creation form
            var result = await controller.Create((int?)null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_ShouldReturnForbid_WhenCompanyIsInactive()
        {
            // Arrange: Create an inactive company
            using var context = GetDatabase();
            var company = await CreateCompany(context, false);
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, manager);

            // Act: Try to open the task creation form
            var result = await controller.Create((int?)null);

            // Assert: Verify that inactive companies are blocked
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_ShouldReturnForbid_WhenCompanyDoesNotExist()
        {
            // Arrange: Create a manager with an invalid company ID
            using var context = GetDatabase();
            var manager = GetManager(999);
            var controller = GetController(context, manager);

            // Act: Try to open the task creation form
            var result = await controller.Create((int?)null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenDueDateIsBeforeStartDate()
        {
            // Arrange: Create a task with invalid dates
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, manager);

            var model = new CreateTaskViewModel
            {
                Title = "Website Development",
                Status = "In Progress",
                Priority = "High",
                StartDate = new DateTime(2026, 12, 20),
                DueDate = new DateTime(2026, 12, 10)
            };

            // Act: Submit the task with invalid dates
            var result = await controller.Create(model);

            // Assert: Verify that the form is returned with a validation error
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("DueDate"));

            // Verify that no task was saved
            Assert.Empty(await context.ProjectTasks.ToListAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenStatusIsInvalid()
        {
            // Arrange: Create a task with an unsupported status
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, manager);

            var model = new CreateTaskViewModel
            {
                Title = "Website Development",
                Status = "Unknown",
                Priority = "High"
            };

            // Act: Submit the task with an invalid status
            var result = await controller.Create(model);

            // Assert: Verify that the task status is rejected
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Status"));

            // Verify that no task was saved
            Assert.Equal(0, await context.ProjectTasks.CountAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenPriorityIsInvalid()
        {
            // Arrange: Create a task with an unsupported priority
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, manager);

            var model = new CreateTaskViewModel
            {
                Title = "Website Development",
                Status = "In Progress",
                Priority = "Urgent"
            };

            // Act: Submit the task with an invalid priority
            var result = await controller.Create(model);

            // Assert: Verify that the priority is rejected
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Priority"));

            // Verify that no task was saved
            Assert.Equal(0, await context.ProjectTasks.CountAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenProjectBelongsToAnotherCompany()
        {
            // Arrange: Create two companies and a manager from company1
            using var context = GetDatabase();
            var company1 = await CreateCompany(context);
            var company2 = await CreateCompany(context);

            var manager = GetManager(company1.CompanyId);
            var controller = GetController(context, manager);

            // Use an invalid project ID to simulate a project unavailable to the manager
            var model = new CreateTaskViewModel
            {
                ProjectId = 999,
                Title = "Website Development",
                Status = "In Progress",
                Priority = "High"
            };

            // Act: Try to create a task for an unavailable project
            var result = await controller.Create(model);

            // Assert: Verify that the project selection is rejected
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.True(controller.ModelState.ContainsKey("ProjectId"));
            Assert.Empty(await context.ProjectTasks.ToListAsync());
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenManagerHasNoCompany()
        {
            // Arrange: Create a manager without a company
            using var context = GetDatabase();
            var manager = GetManager(null);
            var controller = GetController(context, manager);

            // Act: Try to view the task list
            var result = await controller.Index(null, null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnEmptyList_WhenNoTasksExist()
        {
            // Arrange: Create an active company without tasks
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, manager);

            // Act: Request the task list
            var result = await controller.Index(null, null);

            // Assert: Verify that the task list is empty
            var view = Assert.IsType<ViewResult>(result);
            var tasks = Assert.IsAssignableFrom<IEnumerable<ProjectTask>>(view.Model);

            Assert.Empty(tasks);
        }

        [Fact]
        public async Task Edit_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            // Arrange: Create a manager without tasks
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to edit a nonexistent task
            var result = await controller.Edit(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPost_ShouldReturnView_WhenPercentIsNegative()
        {
            // Arrange: Create a task edit form with negative progress
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            var model = new EditTaskViewModel
            {
                ProjectTaskId = 1,
                Title = "Website Development",
                Status = "In Progress",
                Priority = "High",
                Percent = -10
            };

            // Act: Submit the task with negative progress
            var result = await controller.Edit(model);

            // Assert: Verify that negative progress is rejected
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Percent"));
        }

        [Fact]
        public async Task EditPost_ShouldReturnView_WhenPercentExceeds100()
        {
            // Arrange: Create a task edit form with progress above 100%
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            var model = new EditTaskViewModel
            {
                ProjectTaskId = 1,
                Title = "Website Development",
                Status = "In Progress",
                Priority = "High",
                Percent = 150
            };

            // Act: Submit the task with invalid progress
            var result = await controller.Edit(model);

            // Assert: Verify that progress above 100% is rejected
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Percent"));
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            // Arrange: Create a manager without tasks
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Request details for a nonexistent task
            var result = await controller.Details(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Assignments_ShouldReturnForbid_WhenManagerIsInactive()
        {
            // Arrange: Create an inactive manager
            using var context = GetDatabase();
            var manager = GetManager(1, false);
            var controller = GetController(context, manager);

            // Act: Try to access task assignments
            var result = await controller.Assignments(1);

            // Assert: Verify that inactive managers are blocked
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task AssignEmployee_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            // Arrange: Create a manager without tasks
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to assign an employee to a nonexistent task
            var result = await controller.AssignEmployee(999, 1);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(0, await context.TaskAssignments.CountAsync());
        }

        [Fact]
        public async Task RemoveAssignment_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
        {
            // Arrange: Create a manager without assignments
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to remove a nonexistent assignment
            var result = await controller.RemoveAssignment(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Comments_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            // Arrange: Create a manager without tasks
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to view comments for a nonexistent task
            var result = await controller.Comments(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ReplyComment_ShouldReturnNotFound_WhenCommentDoesNotExist()
        {
            // Arrange: Create a manager without comments
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to reply to a nonexistent comment
            var result = await controller.ReplyComment(999, "Test reply");

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteComment_ShouldReturnNotFound_WhenCommentDoesNotExist()
        {
            // Arrange: Create a manager without comments
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to delete a nonexistent comment
            var result = await controller.DeleteComment(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
