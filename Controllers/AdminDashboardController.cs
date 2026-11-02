
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;


[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Display the admin dashboard with company, manager, payment, and subscription statistics
    public async Task<IActionResult> Index()
    {
        // Create a ViewModel to store dashboard information
        var model = new AdminDashboardViewModel();

        // COMPANIES

        // Count all registered companies
        model.TotalCompanies = await _context.Companies.CountAsync();

        // Count companies that are currently active
        model.ActiveCompanies = await _context.Companies.Where(c => c.IsActive).CountAsync();

        // Count companies that are currently inactive
        model.InactiveCompanies = await _context.Companies.Where(c => !c.IsActive).CountAsync();

        // MANAGERS

        // Get all users assigned to the Manager role
        var managers = await _userManager.GetUsersInRoleAsync("Manager");

        // Count all managers
        model.TotalManagers = managers.Count;

        // Count active managers
        model.ActiveManagers = managers.Where(m => m.IsActive).Count();

        // Count inactive managers
        model.InactiveManagers = managers.Where(m => !m.IsActive).Count();

        // PAYMENTS

        // Calculate total revenue from all recorded payments
        // Return 0 if there are no payments
        model.TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

        // Get the first day of the current month
        DateTime firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        // Get the first day of the next month to define the monthly date range
        DateTime firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

        // Calculate total revenue received during the current month
        model.MonthlyRevenue = await _context.Payments.Where(p => p.PaymentDate >= firstDayOfMonth && p.PaymentDate < firstDayOfNextMonth).SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

        // Count the number of payments received during the current month
        model.PaymentsThisMonth = await _context.Payments.Where(p => p.PaymentDate >= firstDayOfMonth && p.PaymentDate < firstDayOfNextMonth).CountAsync();

        // SUBSCRIPTIONS

        // Count companies with at least one payment whose subscription coverage has not expired
        model.ActiveSubscriptions = await _context.Companies.Where(c => c.Payments.Any(p => p.CoverageEndDate >= DateTime.Today)).CountAsync();

        // Count companies that have payment history but no current subscription coverage
        model.ExpiredSubscriptions = await _context.Companies.Where(c => c.Payments.Any() && !c.Payments.Any(p => p.CoverageEndDate >= DateTime.Today)).CountAsync();

        // Count companies that have never made a payment
        model.CompaniesWithoutSubscription = await _context.Companies.Where(c => !c.Payments.Any()).CountAsync();

        // RECENT PAYMENTS

        // Get the 5 most recent payments, including company and subscription plan details
        model.RecentPayments = await _context.Payments.Include(p => p.Company).Include(p => p.SubscriptionPlan).OrderByDescending(p => p.PaymentDate).Take(5).ToListAsync();

        // EXPIRING SUBSCRIPTIONS

        // Calculate the date 30 days from today
        DateTime next30Days = DateTime.Today.AddDays(30);

        // Get payments with subscription coverage ending within the next 30 days
        // Include company and subscription plan details and sort by expiration date
        model.ExpiringSubscriptions = await _context.Payments.Include(p => p.Company).Include(p => p.SubscriptionPlan).Where(p => p.CoverageEndDate >= DateTime.Today && p.CoverageEndDate <= next30Days).OrderBy(p => p.CoverageEndDate).ToListAsync();

        // Send the completed ViewModel to the admin dashboard view
        return View(model);
    }
}
