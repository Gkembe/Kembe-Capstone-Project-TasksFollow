
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;
using Xunit;

namespace TasksFollowTest.Controllers
{
    public class CompanyControllerTests
    {
        // Create a separate in-memory database for each test
        private ApplicationDbContext GetDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Index_ShouldReturnOnlyActiveCompanies()
        {
            // Arrange: Create active and inactive companies
            using var context = GetDatabase();

            context.Companies.Add(new Company
            {
                Name = "Company A",
                Address = "123 Main Street",
                IsActive = true
            });

            context.Companies.Add(new Company
            {
                Name = "Company B",
                Address = "456 Main Street",
                IsActive = false
            });

            await context.SaveChangesAsync();

            var controller = new CompanyController(context);

            // Act: Get companies using the default Active filter
            var result = await controller.Index(null);

            // Assert: Verify that only the active company is returned
            var view = Assert.IsType<ViewResult>(result);

            var companies = Assert.IsAssignableFrom<IEnumerable<Company>>(view.Model);

            Assert.Single(companies);
            Assert.Equal("Company A", companies.First().Name);
        }

        [Fact]
        public async Task Index_ShouldSearchCompaniesByName()
        {
            // Arrange: Create two active companies
            using var context = GetDatabase();

            context.Companies.Add(new Company
            {
                Name = "Microsoft",
                Address = "123 Main Street",
                IsActive = true
            });

            context.Companies.Add(new Company
            {
                Name = "Google",
                Address = "456 Main Street",
                IsActive = true
            });

            await context.SaveChangesAsync();

            var controller = new CompanyController(context);

            // Act: Search for a company named Microsoft
            var result = await controller.Index("Microsoft");

            // Assert: Verify that only Microsoft is returned
            var view = Assert.IsType<ViewResult>(result);
            var companies = Assert.IsAssignableFrom<IEnumerable<Company>>(view.Model);

            Assert.Single(companies);
            Assert.Equal("Microsoft", companies.First().Name);
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange: Create an empty database and controller
            using var context = GetDatabase();
            var controller = new CompanyController(context);

            // Act: Request details for a company that does not exist
            var result = await controller.Details(999);

            // Assert: Verify that the controller returns NotFound
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ShouldSaveCompany_WhenModelIsValid()
        {
            // Arrange: Create a company with valid information
            using var context = GetDatabase();
            var controller = new CompanyController(context);

            var company = new Company
            {
                Name = "TasksFollow Company",
                Description = "Software development company",
                Address = "123 Main Street",
                IsActive = true
            };

            // Act: Submit the company to the Create action
            var result = await controller.Create(company);

            // Assert: Verify that the company was saved successfully
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, await context.Companies.CountAsync());
        }

        [Fact]
        public async Task Activate_ShouldActivateCompany()
        {
            // Arrange: Create an inactive company
            using var context = GetDatabase();

            var company = new Company
            {
                Name = "Company A",
                Address = "123 Main Street",
                IsActive = false
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var controller = new CompanyController(context);

            // Act: Activate the company
            var result = await controller.Activate(company.CompanyId);

            // Assert: Verify that the company is now active
            Assert.IsType<RedirectToActionResult>(result);
            Assert.True(company.IsActive);
        }

        [Fact]
        public async Task Deactivate_ShouldDeactivateCompany()
        {
            // Arrange: Create an active company
            using var context = GetDatabase();

            var company = new Company
            {
                Name = "Company A",
                Address = "123 Main Street",
                IsActive = true
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var controller = new CompanyController(context);

            // Act: Deactivate the company
            var result = await controller.Deactivate(company.CompanyId);

            // Assert: Verify that the company is now inactive
            Assert.IsType<RedirectToActionResult>(result);
            Assert.False(company.IsActive);
        }
    }
}
