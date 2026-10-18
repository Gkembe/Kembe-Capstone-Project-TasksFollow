using System.ComponentModel.Design;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Data;
using TasksFollow.Models;


namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Admin")]
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

            var users = await _context.Users
                .Where(u => managerIds.Contains(u.Id))
                .Include(u => u.Company)
                .ToListAsync();

            var managerList = new List<ManagerIndexViewModel>();

            foreach (var user in users)
            {
                var manager = new ManagerIndexViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CompanyId = user.CompanyID
                   
                };


                if (user.Company != null)
                {
                    manager.CompanyName = user.Company.Name;
                }
                else
                {
                    manager.CompanyName = "No Company";
                }

                managerList.Add(manager);
            }

            return View(managerList);
        }

        public async Task<IActionResult> Create(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);

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

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            var manager = new ManagerEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CompanyId = user.CompanyID
            };

            var companies = await _context.Companies.Where(c => c.IsActive).ToListAsync();

            foreach (var company in companies)
            {
                manager.Companies.Add(new SelectListItem
                {
                    Value = company.CompanyId.ToString(),
                    Text = company.Name
                });
            }

            return View(manager);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ManagerEditViewModel manager)
        {
            if (!ModelState.IsValid)
            {
                var companies = await _context.Companies.Where(c => c.IsActive).ToListAsync();

                foreach (var company in companies)
                {
                    manager.Companies.Add(new SelectListItem
                    {
                        Value = company.CompanyId.ToString(),

                        Text = company.Name
                    });
                }

                return View(manager);
            }

            var user = await _userManager.FindByIdAsync(manager.Id);

            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = manager.FirstName;
            user.MiddleName = manager.MiddleName;
            user.LastName = manager.LastName;
            user.Email = manager.Email;
            user.UserName = manager.Email;
            user.PhoneNumber = manager.PhoneNumber;
            user.CompanyID = manager.CompanyId;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(manager);
            }

            return RedirectToAction("Details", new { id = user.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            var manager = new ManagerDetailsViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,

                CompanyId = user.CompanyID,

                
            };

            if (user.Company != null)
            {
                manager.CompanyName = user.Company.Name;

                manager.CompanyIsActive = user.Company.IsActive;
            }
            else
            {
                manager.CompanyName = "No Company";

                manager.CompanyIsActive = false;
            }

            return View(manager);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var manager = await _userManager.FindByIdAsync(id);

            if (manager == null)
            {
                return NotFound();
            }

            var isManager = await _userManager.IsInRoleAsync(manager, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            manager.IsActive = false;

            await _userManager.UpdateAsync(manager);

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var manager = await _userManager.FindByIdAsync(id);

            if (manager == null)
            {
                return NotFound();
            }

            var isManager = await _userManager.IsInRoleAsync(manager, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            manager.IsActive = true;

            await _userManager.UpdateAsync(manager);

            return RedirectToAction(nameof(Index));
        }



    }
}
