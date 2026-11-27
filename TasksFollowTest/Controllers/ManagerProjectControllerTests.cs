
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
    public class ManagerProjectControllerTests
    {
        // Create a separate in-memory database for each test
        private ApplicationDbContext GetDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // Create a fake UserManager to simulate logged-in managers
        private Mock<UserManager<ApplicationUser>> GetUserManager()
        {
            // Create a fake user store
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
        private ManagerProjectController GetController(ApplicationDbContext context, ApplicationUser? manager)
        {
            var userManager = GetUserManager();

            // Return the manager when the controller requests the current user
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(manager);

            // EmailService is not used in these test scenarios
            var controller = new ManagerProjectController(context, userManager.Object, null!);

            // Create a fake HTTP context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

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
        private ApplicationUser GetManager(int? companyId = 1)
        {
            return new ApplicationUser
            {
                Id = "manager1",
                FirstName = "John",
                LastName = "Smith",
                UserName = "manager@example.com",
                Email = "manager@example.com",
                CompanyID = companyId,
                IsActive = true
            };
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenManagerIsNotLoggedIn()
        {
            // Arrange: Simulate a manager who is not logged in
            using var context = GetDatabase();
            var controller = GetController(context, null);

            // Act: Try to open the project list
            var result = await controller.Index(null, null, null, null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnForbid_WhenManagerHasNoCompany()
        {
            // Arrange: Create a manager without a company
            using var context = GetDatabase();
            var manager = GetManager(null);
            var controller = GetController(context, manager);

            // Act: Try to open the project list
            var result = await controller.Index(null, null, null, null);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Index_ShouldReturnEmptyList_WhenNoProjectsExist()
        {
            // Arrange: Create an active company without projects
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, manager);

            // Act: Request the project list
            var result = await controller.Index(null, null, null, null);

            // Assert: Verify that the project list is empty
            var view = Assert.IsType<ViewResult>(result);
            var projects = Assert.IsAssignableFrom<IEnumerable<Project>>(view.Model);

            Assert.Empty(projects);
        }

        [Fact]
        public async Task Index_ShouldSearchProjectsByName()
        {
            // Arrange: Create an active company and two projects
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var manager = GetManager(company.CompanyId);

            // Save the manager as the project creator
            context.Users.Add(manager);

            context.Projects.Add(new Project
            {
                Name = "Website Development",
                Description = "Build company website",
                Status = "In Progress",
                CompanyId = company.CompanyId,
                CreatedByUserId = manager.Id
            });

            context.Projects.Add(new Project
            {
                Name = "Mobile Application",
                Description = "Build mobile application",
                Status = "Not Started",
                CompanyId = company.CompanyId,
                CreatedByUserId = manager.Id
            });

            await context.SaveChangesAsync();

            var controller = GetController(context, manager);

            // Act: Search for Website Development
            var result = await controller.Index("Website", null, null, null);

            // Assert: Verify that only the matching project is returned
            var view = Assert.IsType<ViewResult>(result);
            var projects = Assert.IsAssignableFrom<IEnumerable<Project>>(view.Model);

            Assert.Single(projects);
            Assert.Equal("Website Development", projects.First().Name);
        }

        [Fact]
        public void Create_ShouldReturnView()
        {
            // Arrange: Create the controller
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Open the project creation form
            var result = controller.Create();

            // Assert: Verify that an empty form is returned
            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<CreateProjectViewModel>(view.Model);
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenDueDateIsBeforeStartDate()
        {
            // Arrange: Create a project with invalid dates
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            var model = new CreateProjectViewModel
            {
                Name = "Website Development",
                Status = "In Progress",
                StartDate = new DateTime(2026, 12, 20),
                DueDate = new DateTime(2026, 12, 10)
            };

            // Act: Submit the project with invalid dates
            var result = await controller.Create(model);

            // Assert: Verify that the creation form is returned
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);

            // Verify that the due date has a validation error
            Assert.True(controller.ModelState.ContainsKey("DueDate"));

            // Verify that no project was saved
            Assert.Equal(0, await context.Projects.CountAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenStatusIsInvalid()
        {
            // Arrange: Create an active company and manager
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, manager);

            var model = new CreateProjectViewModel
            {
                Name = "Website Development",
                Status = "Unknown"
            };

            // Act: Submit a project with an unsupported status
            var result = await controller.Create(model);

            // Assert: Verify that the form is returned with an error
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Status"));

            // Verify that the invalid project was not saved
            Assert.Empty(await context.Projects.ToListAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldReturnForbid_WhenCompanyIsInactive()
        {
            // Arrange: Create an inactive company
            using var context = GetDatabase();
            var company = await CreateCompany(context, false);
            var manager = GetManager(company.CompanyId);
            var controller = GetController(context, manager);

            var model = new CreateProjectViewModel
            {
                Name = "Website Development",
                Status = "In Progress"
            };

            // Act: Try to create a project for an inactive company
            var result = await controller.Create(model);

            // Assert: Verify that access is forbidden
            Assert.IsType<ForbidResult>(result);
            Assert.Equal(0, await context.Projects.CountAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldSaveProject_WhenModelIsValid()
        {
            // Arrange: Create an active company and manager
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var manager = GetManager(company.CompanyId);

            // Save the manager as the project creator
            context.Users.Add(manager);
            await context.SaveChangesAsync();

            var controller = GetController(context, manager);

            var model = new CreateProjectViewModel
            {
                Name = "Website Development",
                Description = "Create a company website",
                Status = "In Progress",
                StartDate = new DateTime(2026, 12, 1),
                DueDate = new DateTime(2026, 12, 20)
            };

            // Act: Submit the valid project
            var result = await controller.Create(model);

            // Assert: Verify that the controller redirects to Index
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Verify that the project was saved
            var project = await context.Projects.SingleAsync();

            Assert.Equal("Website Development", project.Name);
            Assert.Equal("In Progress", project.Status);
            Assert.Equal(company.CompanyId, project.CompanyId);
            Assert.Equal(manager.Id, project.CreatedByUserId);
        }

        [Fact]
        public async Task Edit_ShouldReturnNotFound_WhenProjectDoesNotExist()
        {
            // Arrange: Create a manager without projects
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to edit a nonexistent project
            var result = await controller.Edit(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPost_ShouldReturnView_WhenDatesAreInvalid()
        {
            // Arrange: Create a project edit form with invalid dates
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            var model = new EditProjectViewModel
            {
                ProjectId = 1,
                Name = "Website Development",
                Status = "In Progress",
                StartDate = new DateTime(2026, 12, 20),
                DueDate = new DateTime(2026, 12, 10)
            };

            // Act: Submit the invalid project changes
            var result = await controller.Edit(model);

            // Assert: Verify that the edit form is returned
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("DueDate"));
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenProjectDoesNotExist()
        {
            // Arrange: Create a manager without projects
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Request details for a nonexistent project
            var result = await controller.Details(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Members_ShouldReturnNotFound_WhenProjectDoesNotExist()
        {
            // Arrange: Create a manager without projects
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to view members of a nonexistent project
            var result = await controller.Members(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddMember_ShouldReturnNotFound_WhenProjectDoesNotExist()
        {
            // Arrange: Create an active manager without projects
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to add an employee to a nonexistent project
            var result = await controller.AddMember(999, 1);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);

            // Verify that no project membership was created
            Assert.Equal(0, await context.ProjectMembers.CountAsync());
        }

        [Fact]
        public async Task RemoveMember_ShouldReturnNotFound_WhenMembershipDoesNotExist()
        {
            // Arrange: Create an active manager without memberships
            using var context = GetDatabase();
            var manager = GetManager();
            var controller = GetController(context, manager);

            // Act: Try to remove a nonexistent project membership
            var result = await controller.RemoveMember(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
