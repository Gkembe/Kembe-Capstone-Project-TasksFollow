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


    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel();

        // Companies
        model.TotalCompanies = await _context.Companies.CountAsync();
        model.ActiveCompanies = await _context.Companies.Where(c => c.IsActive).CountAsync();
        model.InactiveCompanies = await _context.Companies.Where(c => !c.IsActive).CountAsync();

        // Managers
        var managers = await _userManager.GetUsersInRoleAsync("Manager");

        model.TotalManagers = managers.Count;
        model.ActiveManagers = managers.Where(m => m.IsActive).Count();
        model.InactiveManagers = managers.Where(m => !m.IsActive).Count();

        // Payments
        model.TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

        // Payments for the current month
        DateTime firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTime firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

        model.MonthlyRevenue = await _context.Payments
            .Where(p => p.PaymentDate >= firstDayOfMonth && p.PaymentDate < firstDayOfNextMonth)
            .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

        model.PaymentsThisMonth = await _context.Payments
            .Where(p => p.PaymentDate >= firstDayOfMonth && p.PaymentDate < firstDayOfNextMonth)
            .CountAsync();

        // Active subscriptions
        model.ActiveSubscriptions = await _context.Companies
            .Where(c => c.Payments.Any(p => p.CoverageEndDate >= DateTime.Today))
            .CountAsync();

        // Expired subscriptions
        model.ExpiredSubscriptions = await _context.Companies
            .Where(c => c.Payments.Any() &&
                        !c.Payments.Any(p => p.CoverageEndDate >= DateTime.Today))
            .CountAsync();

        // Companies that have never made a payment
        model.CompaniesWithoutSubscription = await _context.Companies
            .Where(c => !c.Payments.Any())
            .CountAsync();

        // Recent payments
        model.RecentPayments = await _context.Payments
            .Include(p => p.Company)
            .Include(p => p.SubscriptionPlan)
            .OrderByDescending(p => p.PaymentDate)
            .Take(5)
            .ToListAsync();

        // Subscriptions expiring within the next 30 days
        DateTime next30Days = DateTime.Today.AddDays(30);

        model.ExpiringSubscriptions = await _context.Payments
            .Include(p => p.Company)
            .Include(p => p.SubscriptionPlan)
            .Where(p => p.CoverageEndDate >= DateTime.Today &&
                        p.CoverageEndDate <= next30Days)
            .OrderBy(p => p.CoverageEndDate)
            .ToListAsync();

        return View(model);
    }
}