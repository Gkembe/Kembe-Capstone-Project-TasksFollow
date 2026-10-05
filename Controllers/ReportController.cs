using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Report
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? companyId, int? subscriptionPlanId)
        {
            // Start payment query
            var query = _context.Payments
                .Include(p => p.Company)
                .Include(p => p.SubscriptionPlan)
                .AsQueryable();

            // Filter by start date
            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);

            // Filter by end date
            if (endDate.HasValue)
            {
                DateTime nextDay = endDate.Value.AddDays(1);
                query = query.Where(p => p.PaymentDate < nextDay);
            }

            // Filter by company
            if (companyId.HasValue)
                query = query.Where(p => p.CompanyId == companyId.Value);

            // Filter by subscription plan
            if (subscriptionPlanId.HasValue)
                query = query.Where(p => p.SubscriptionPlanId == subscriptionPlanId.Value);

            // Get payments
            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // Create report
            var model = new ReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                CompanyId = companyId,
                SubscriptionPlanId = subscriptionPlanId,
                Payments = payments,
                TotalPayments = payments.Count,
                TotalRevenue = payments.Sum(p => p.AmountPaid),
                CompaniesPaid = payments.Select(p => p.CompanyId).Distinct().Count()
            };

            // Dropdowns
            ViewBag.Companies = new SelectList(
                await _context.Companies.OrderBy(c => c.Name).ToListAsync(), "CompanyId", "Name", companyId);

            ViewBag.Plans = new SelectList(
                await _context.SubscriptionPlans.OrderBy(p => p.Name).ToListAsync(), "SubscriptionPlanId", "Name", subscriptionPlanId);

            return View(model);
        }
    }
}