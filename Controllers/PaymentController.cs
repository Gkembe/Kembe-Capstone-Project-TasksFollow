using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;
using TasksFollow.Models.ViewModels;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }


        // Display all subscription plans
        public async Task<IActionResult> Plans()
        {
            var plans = await _context.SubscriptionPlans.ToListAsync();

            return View(plans);
        }


        // Display create plan page
        public IActionResult CreatePlan()
        {
            return View();
        }


        // Create subscription plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(SubscriptionPlan plan)
        {
            if (ModelState.IsValid)
            {
                _context.SubscriptionPlans.Add(plan);

                await _context.SaveChangesAsync();

                return RedirectToAction("Plans");
            }

            return View(plan);
        }

        [HttpGet]

        public async Task<IActionResult> EditPlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlan(SubscriptionPlan plan)
        {
            if (ModelState.IsValid)
            {
                _context.SubscriptionPlans.Update(plan);

                await _context.SaveChangesAsync();

                return RedirectToAction("Plans");
            }

            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivatePlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);

            if (plan == null)
            {
                return NotFound();
            }

            plan.IsActive = false;

            await _context.SaveChangesAsync();

            return RedirectToAction("Plans");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivatePlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);

            if (plan == null)
            {
                return NotFound();
            }

            plan.IsActive = true;

            await _context.SaveChangesAsync();

            return RedirectToAction("Plans");
        }


        //PAYMENT 

        [HttpGet]
        public async Task<IActionResult> Create(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null)
            {
                return NotFound();
            }

            var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();

            ViewBag.Plans = new SelectList(plans, "SubscriptionPlanId", "Name");

            var model = new PaymentCreateViewModel
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentCreateViewModel model)
        {
            // Find the company
            var company = await _context.Companies.FindAsync(model.CompanyId);

            if (company == null) 
            { 
                
                return NotFound();
            }

            // Find the selected subscription plan

            var plan = await _context.SubscriptionPlans.FindAsync(model.SubscriptionPlanId);

            if (plan == null) 
            { 
                
                ModelState.AddModelError("SubscriptionPlanId", "Please select a valid subscription plan.");
            
            }

            // Return to the form if validation fails
            if (!ModelState.IsValid)
            {
                model.CompanyName = company.Name;
                var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();
                ViewBag.Plans = new SelectList(plans, "SubscriptionPlanId", "Name");
                return View(model);
            }

            // Get the plan price and payment amount
            decimal monthlyPrice = plan!.MonthlyPrice!.Value;

            decimal amountPaid = model.AmountPaid!.Value;

            // Calculate how many months the payment covers
            int monthsPaid = (int)(amountPaid / monthlyPrice);

            // Payment must cover at least one month
            if (monthsPaid < 1)
            {
                ModelState.AddModelError("AmountPaid", "The payment must cover at least one month.");

                model.CompanyName = company.Name;

                var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();
                ViewBag.Plans = new SelectList(plans, "SubscriptionPlanId", "Name");

                return View(model);
            }

            // Payment must pay for complete months
            if (amountPaid % monthlyPrice != 0)
            {
                ModelState.AddModelError("AmountPaid", $"Payment must be a multiple of ${monthlyPrice:0.00}.");

                model.CompanyName = company.Name;

                var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();

                ViewBag.Plans = new SelectList(plans, "SubscriptionPlanId", "Name");

                return View(model);
            }

            // Get the company's latest subscription payment

            var lastPayment = await _context.Payments
                .Where(p => p.CompanyId == company.CompanyId)
                .OrderByDescending(p => p.CoverageEndDate)
                .FirstOrDefaultAsync();

            // Start coverage today by default
            DateTime coverageStartDate = DateTime.Today;

            // If the company still has active coverage, extend from its current end date
            if (lastPayment != null && lastPayment.CoverageEndDate > DateTime.Today)
            {
                coverageStartDate = lastPayment.CoverageEndDate;

            }
                

            // Calculate the new subscription end date
            DateTime coverageEndDate = coverageStartDate.AddMonths(monthsPaid);

            // Create the payment
            var payment = new Payment
            {
                CompanyId = company.CompanyId,
                SubscriptionPlanId = plan.SubscriptionPlanId,
                AmountPaid = amountPaid,
                MonthlyPrice = monthlyPrice,
                MonthsPaid = monthsPaid,
                PaymentDate = DateTime.Now,
                CoverageStartDate = coverageStartDate,
                CoverageEndDate = coverageEndDate
            };

            // Save the payment
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Return to the company's details page
            return RedirectToAction("Details", "Company", new { id = company.CompanyId });
        }

        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Company)
                .Include(p => p.SubscriptionPlan)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        // GET: Payment/CompanyHistory/5
        public async Task<IActionResult> CompanyHistory(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null)
            {
                return NotFound();
            }

            var payments = await _context.Payments
                .Include(p => p.SubscriptionPlan)
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.CompanyId = company.CompanyId;

            ViewBag.CompanyName = company.Name;

            return View(payments);
        }

        // GET: Payment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Company)
                .Include(p => p.SubscriptionPlan)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            int companyId = payment.CompanyId;

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return RedirectToAction("CompanyHistory", new { companyId = companyId });
        }
    }
}