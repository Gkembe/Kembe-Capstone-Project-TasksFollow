using System.ComponentModel.Design;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;

namespace TasksFollow.Controllers
{
    public class ManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ManagerController(UserManager<ApplicationUser> userManager,ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var managers = await _userManager
                .GetUsersInRoleAsync("Manager");

            var managerIds = managers
                .Select(m => m.Id)
                .ToList();

            var managerList = await _context.Users
                .Where(u => managerIds.Contains(u.Id))
                .Include(u => u.Company)
                .Select(u => new ManagerIndexViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    MiddleName = u.MiddleName,
                    Email = u.Email ?? "",
                    CompanyId = u.CompanyID,
                    CompanyName = u.Company != null
                        ? u.Company.Name
                        : "No Company"
                })
                .ToListAsync();

            return View(managerList);
        }

        public async Task<IActionResult> Create(int Id)
        {
            var company = await _context.Companies.FindAsync(Id);

            if (company == null)
            {
                return NotFound();
            }

            var model = new Manager
            {
                CompanyId = company.CompanyId,
                
            };

            ViewBag.CompanyName = company.Name;
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Manager model)
        {
            var company = await _context.Companies.FindAsync(model.CompanyId);

            if (company == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CompanyName = company.Name;
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");

                ViewBag.CompanyName = company.Name;

                return View(model);
            }

            var manager = new ApplicationUser
            {
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,

                UserName = model.Email,
                Email = model.Email,

                CompanyID = company.CompanyId,

                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(manager, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(manager,"Manager");

                return RedirectToAction("Details", "Company",new { id = company.CompanyId });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty,error.Description);
            }

            ViewBag.CompanyName = company.Name;

            return View(model);
        }



    }
}
