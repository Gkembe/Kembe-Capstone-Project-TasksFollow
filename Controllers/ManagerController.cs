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

       
        public ManagerController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Manager/Index
        // Show all users in the Manager role and their company information.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Retrieve managers through ASP.NET Core Identity.
            var managers = await _userManager.GetUsersInRoleAsync("Manager");

            // Collect manager IDs to load their additional database details.
            var managerIds = managers.Select(m => m.Id).ToList();

            // Load manager accounts with their related companies.
            var users = await _context.Users.Where(u => managerIds.Contains(u.Id)).Include(u => u.Company).ToListAsync();

            var managerList = new List<ManagerIndexViewModel>();

            // Convert each user into a ViewModel for the managers list.
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

                // Display the company name, or a fallback if none is linked.
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




        // GET: Manager/Create
        // Open the form to create a manager for a specific company.
        public async Task<IActionResult> Create(int companyId)
        {
            // Confirm that the selected company exists.
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null)
            {
                return NotFound();
            }

            // Preselect the company when creating the manager.
            var model = new Manager
            {
                CompanyId = company.CompanyId
            };

            // Show the company name on the creation form.
            ViewBag.CompanyName = company.Name;

            return View(model);
        }



        // POST: Manager/Create
        // Validate the form and create a new Identity user in the Manager role.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Manager model)
        {
            // Make sure the manager will be associated with an existing company.
            var company = await _context.Companies.FindAsync(model.CompanyId);

            if (company == null)
            {
                return NotFound();
            }

            // Redisplay the form when the submitted data is invalid.
            if (!ModelState.IsValid)
            {
                ViewBag.CompanyName = company.Name;
                return View(model);
            }

            // Prevent creating two accounts with the same email address.
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");

                ViewBag.CompanyName = company.Name;

                return View(model);
            }

            // Prepare the new manager's Identity account and company association.
            var manager = new ApplicationUser
            {
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                CompanyID = company.CompanyId,
                EmailConfirmed = true,
                IsActive = true
            };

            // Create the account using the submitted password.
            var result = await _userManager.CreateAsync(manager, model.Password);

            if (result.Succeeded)
            {
                // Assign the Manager role and return to the company's details page.
                await _userManager.AddToRoleAsync(manager, "Manager");
                return RedirectToAction("Details", "Company", new { id = company.CompanyId });
            }

            // Display any Identity validation or account creation errors.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.CompanyName = company.Name;
            return View(model);
        }



        // GET: Manager/Edit/{id}
        // Load an existing manager and the companies available for selection.
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            // Require a manager ID.
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Do not allow this form to edit users outside the Manager role.
            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            // Copy the manager's existing information into the edit form.
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

            // Populate the company dropdown with active companies.
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



        // POST: Manager/Edit
        // Save changes to an existing manager's profile and company association.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ManagerEditViewModel manager)
        {
            // Rebuild the company dropdown if validation fails.
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

            // Find the Identity account being edited.
            var user = await _userManager.FindByIdAsync(manager.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Apply the submitted profile information to the account.
            user.FirstName = manager.FirstName;
            user.MiddleName = manager.MiddleName;
            user.LastName = manager.LastName;
            user.Email = manager.Email;
            user.UserName = manager.Email;
            user.PhoneNumber = manager.PhoneNumber;
            user.CompanyID = manager.CompanyId;

            // Save the updated Identity user.
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                // Display errors if Identity rejects the update.
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(manager);
            }

            return RedirectToAction("Details", new { id = user.Id });
        }



        // GET: Manager/Details/{id}
        // Show a manager's profile and associated company details.
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // Find the user and load company information.
            var user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Verify that this account actually belongs to the Manager role.
            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            // Prepare the manager's information for the details page.
            var manager = new ManagerDetailsViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                CompanyId = user.CompanyID
            };

            // Add company name and status when a company is linked.
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



        // POST: Manager/Deactivate
        // Disable a manager account without deleting it from the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            // Confirm that an account ID was supplied.
            if (id == null)
            {
                return NotFound();
            }

            var manager = await _userManager.FindByIdAsync(id);

            if (manager == null)
            {
                return NotFound();
            }

            // Make sure the target account belongs to the Manager role.
            var isManager = await _userManager.IsInRoleAsync(manager, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            // Set the manager's application-level active flag to false.
            manager.IsActive = false;
            await _userManager.UpdateAsync(manager);

            return RedirectToAction(nameof(Index));
        }

        // POST: Manager/Activate
        // Reactivate a previously deactivated manager account.
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

            // Ensure only Manager-role accounts can be activated here.
            var isManager = await _userManager.IsInRoleAsync(manager, "Manager");

            if (!isManager)
            {
                return NotFound();
            }

            // Restore the manager's application-level active flag.
            manager.IsActive = true;
            await _userManager.UpdateAsync(manager);

            return RedirectToAction(nameof(Index));
        }

    }

}
