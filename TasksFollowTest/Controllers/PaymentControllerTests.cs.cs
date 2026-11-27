
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Controllers;
using TasksFollow.Data;
using TasksFollow.Models;
using TasksFollow.Models.ViewModels;
using Xunit;

namespace TasksFollowTest.Controllers
{
    public class PaymentControllerTests
    {
        // Create a separate in-memory database for each test
        private ApplicationDbContext GetDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // Create and save a company for testing
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

        // Create and save a subscription plan for testing
        private async Task<SubscriptionPlan> CreateSubscriptionPlan(ApplicationDbContext context, bool isActive = true)
        {
            var plan = new SubscriptionPlan
            {
                Name = "Premium Plan",
                MonthlyPrice = 100,
                IsActive = isActive
            };

            context.SubscriptionPlans.Add(plan);
            await context.SaveChangesAsync();

            return plan;
        }

        [Fact]
        public async Task Plans_ShouldReturnEmptyList_WhenNoPlansExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            // Act: Request the subscription plan list
            var result = await controller.Plans();

            // Assert: Verify that no plans are returned
            var view = Assert.IsType<ViewResult>(result);
            var plans = Assert.IsAssignableFrom<IEnumerable<SubscriptionPlan>>(view.Model);

            Assert.Empty(plans);
        }

        [Fact]
        public void CreatePlan_ShouldReturnView()
        {
            // Arrange: Create the controller
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            // Act: Open the subscription plan creation form
            var result = controller.CreatePlan();

            // Assert: Verify that the form is returned
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task CreatePlan_ShouldNotSave_WhenModelStateIsInvalid()
        {
            // Arrange: Create an invalid subscription plan
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            var plan = new SubscriptionPlan
            {
                Name = "",
                MonthlyPrice = 100,
                IsActive = true
            };

            // Simulate a server-side validation error
            controller.ModelState.AddModelError("Name", "Plan name is required.");

            // Act: Submit the invalid subscription plan
            var result = await controller.CreatePlan(plan);

            // Assert: Verify that the form is returned
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(plan, view.Model);
            Assert.False(controller.ModelState.IsValid);

            // Verify that no plan was saved
            Assert.Equal(0, await context.SubscriptionPlans.CountAsync());
        }

        [Fact]
        public async Task EditPlan_ShouldReturnNotFound_WhenPlanDoesNotExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            // Act: Try to edit a nonexistent subscription plan
            var result = await controller.EditPlan(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ActivatePlan_ShouldActivateSubscriptionPlan()
        {
            // Arrange: Create an inactive subscription plan
            using var context = GetDatabase();
            var plan = await CreateSubscriptionPlan(context, false);
            var controller = new PaymentController(context);

            // Act: Activate the subscription plan
            var result = await controller.ActivatePlan(plan.SubscriptionPlanId);

            // Assert: Verify that the plan is active
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Plans", redirect.ActionName);
            Assert.True(plan.IsActive);
        }

        [Fact]
        public async Task DeactivatePlan_ShouldDeactivateSubscriptionPlan()
        {
            // Arrange: Create an active subscription plan
            using var context = GetDatabase();
            var plan = await CreateSubscriptionPlan(context);
            var controller = new PaymentController(context);

            // Act: Deactivate the subscription plan
            var result = await controller.DeactivatePlan(plan.SubscriptionPlanId);

            // Assert: Verify that the plan is inactive
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Plans", redirect.ActionName);
            Assert.False(plan.IsActive);
        }

        [Fact]
        public async Task Create_ShouldReturnNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            // Act: Open the payment form for a nonexistent company
            var result = await controller.Create(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreatePost_ShouldReturnNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange: Create a payment for a nonexistent company
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            var model = new PaymentCreateViewModel
            {
                CompanyId = 999,
                SubscriptionPlanId = 1,
                AmountPaid = 100
            };

            // Act: Submit the payment
            var result = await controller.Create(model);

            // Assert: Verify that the company must exist
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreatePost_ShouldReturnView_WhenPlanDoesNotExist()
        {
            // Arrange: Create a company without subscription plans
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var controller = new PaymentController(context);

            var model = new PaymentCreateViewModel
            {
                CompanyId = company.CompanyId,
                SubscriptionPlanId = 999,
                AmountPaid = 100
            };

            // Act: Submit a payment with an invalid plan
            var result = await controller.Create(model);

            // Assert: Verify that the payment is rejected
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("SubscriptionPlanId"));

            // Verify that no payment was saved
            Assert.Equal(0, await context.Payments.CountAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldRejectPayment_WhenAmountIsLessThanMonthlyPrice()
        {
            // Arrange: Create a company and a $100 monthly plan
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var plan = await CreateSubscriptionPlan(context);
            var controller = new PaymentController(context);

            var model = new PaymentCreateViewModel
            {
                CompanyId = company.CompanyId,
                SubscriptionPlanId = plan.SubscriptionPlanId,
                AmountPaid = 50
            };

            // Act: Submit a payment that covers less than one month
            var result = await controller.Create(model);

            // Assert: Verify that the payment is rejected
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.True(controller.ModelState.ContainsKey("AmountPaid"));

            // Verify that no payment was recorded
            Assert.Empty(await context.Payments.ToListAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldRejectPayment_WhenAmountIsNotMultipleOfMonthlyPrice()
        {
            // Arrange: Create a company and a $100 monthly plan
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var plan = await CreateSubscriptionPlan(context);
            var controller = new PaymentController(context);

            var model = new PaymentCreateViewModel
            {
                CompanyId = company.CompanyId,
                SubscriptionPlanId = plan.SubscriptionPlanId,
                AmountPaid = 150
            };

            // Act: Submit a payment that does not cover complete months
            var result = await controller.Create(model);

            // Assert: Verify that the payment amount is rejected
            var view = Assert.IsType<ViewResult>(result);

            Assert.Same(model, view.Model);
            Assert.True(controller.ModelState.ContainsKey("AmountPaid"));

            // Verify that no payment was saved
            Assert.Equal(0, await context.Payments.CountAsync());
        }

        [Fact]
        public async Task CreatePost_ShouldSavePayment_WhenAmountIsValid()
        {
            // Arrange: Create a company and a $100 monthly plan
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var plan = await CreateSubscriptionPlan(context);
            var controller = new PaymentController(context);

            var model = new PaymentCreateViewModel
            {
                CompanyId = company.CompanyId,
                SubscriptionPlanId = plan.SubscriptionPlanId,
                AmountPaid = 200
            };

            // Act: Record a payment covering two months
            var result = await controller.Create(model);

            // Assert: Verify that the controller redirects to Company Details
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Company", redirect.ControllerName);

            // Verify that the payment was saved correctly
            var payment = await context.Payments.SingleAsync();

            Assert.Equal(company.CompanyId, payment.CompanyId);
            Assert.Equal(plan.SubscriptionPlanId, payment.SubscriptionPlanId);
            Assert.Equal(200m, payment.AmountPaid);
            Assert.Equal(100m, payment.MonthlyPrice);
            Assert.Equal(2, payment.MonthsPaid);

            // Verify that two months of coverage were added
            Assert.Equal(DateTime.Today, payment.CoverageStartDate);
            Assert.Equal(DateTime.Today.AddMonths(2), payment.CoverageEndDate);
        }

        [Fact]
        public async Task CreatePost_ShouldExtendCoverage_WhenCompanyHasActiveSubscription()
        {
            // Arrange: Create a company with an active subscription
            using var context = GetDatabase();
            var company = await CreateCompany(context);
            var plan = await CreateSubscriptionPlan(context);

            var previousEndDate = DateTime.Today.AddMonths(1);

            // Save an existing payment with future coverage
            context.Payments.Add(new Payment
            {
                CompanyId = company.CompanyId,
                SubscriptionPlanId = plan.SubscriptionPlanId,
                AmountPaid = 100,
                MonthlyPrice = 100,
                MonthsPaid = 1,
                PaymentDate = DateTime.Now,
                CoverageStartDate = DateTime.Today,
                CoverageEndDate = previousEndDate
            });

            await context.SaveChangesAsync();

            var controller = new PaymentController(context);

            var model = new PaymentCreateViewModel
            {
                CompanyId = company.CompanyId,
                SubscriptionPlanId = plan.SubscriptionPlanId,
                AmountPaid = 200
            };

            // Act: Record an additional two-month payment
            var result = await controller.Create(model);

            // Assert: Verify that the payment was saved
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(2, await context.Payments.CountAsync());

            // Get the new payment
            var payment = await context.Payments
                .OrderByDescending(p => p.PaymentId)
                .FirstAsync();

            // Verify that coverage starts when the old subscription ends
            Assert.Equal(previousEndDate, payment.CoverageStartDate);

            // Verify that two additional months were added
            Assert.Equal(previousEndDate.AddMonths(2), payment.CoverageEndDate);
            Assert.Equal(2, payment.MonthsPaid);
        }

        [Fact]
        public async Task CompanyHistory_ShouldReturnNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            // Act: Request payment history for a nonexistent company
            var result = await controller.CompanyHistory(999, null!, null);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenPaymentDoesNotExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            // Act: Try to open the delete page for a nonexistent payment
            var result = await controller.Delete(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_ShouldReturnNotFound_WhenPaymentDoesNotExist()
        {
            // Arrange: Create an empty database
            using var context = GetDatabase();
            var controller = new PaymentController(context);

            // Act: Try to delete a nonexistent payment
            var result = await controller.DeleteConfirmed(999);

            // Assert: Verify that NotFound is returned
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
