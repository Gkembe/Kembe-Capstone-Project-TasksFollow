
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Models;
using TasksFollow.Data;
using Microsoft.AspNetCore.Authorization;

[Authorize (Roles = "Admin")]
public class CompanyController : Controller
{
    private readonly ApplicationDbContext _context;

    public CompanyController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: COMPANYS
    public async Task<IActionResult> Index(string search, string status = "Active")
    {
        // Get companies from the database
        var companies = _context.Companies.AsQueryable();

        // Search by company name
        if (!string.IsNullOrWhiteSpace(search))
        {
            companies = companies.Where(c => c.Name.Contains(search));
        }

        // Filter companies by status (Active by default)
        if (status == "Active")
        {
            companies = companies.Where(c => c.IsActive);
        }
        else if (status == "Inactive")
        {
            companies = companies.Where(c => !c.IsActive);
        }

        // Keep the selected search and filter values
        ViewBag.Search = search;
        ViewBag.Status = status;

        return View(await companies.OrderBy(c => c.Name).ToListAsync());
    }

    // GET: Companies/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var company = await _context.Companies
            .Include(c => c.Payments)
            .ThenInclude(p => p.SubscriptionPlan)
            .FirstOrDefaultAsync(c => c.CompanyId == id);

        if (company == null)
        {
            return NotFound();
        }

        return View(company);
    }

    // GET: COMPANYS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: COMPANYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CompanyId,Name,Description,Phone,Email,Address,CreatedDate,IsActive")] Company company)
    {
        if (ModelState.IsValid)
        {
            _context.Add(company);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(company);
    }

    // GET: COMPANYS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var company = await _context.Companies.FindAsync(id);
        if (company == null)
        {
            return NotFound();
        }
        return View(company);
    }

    // POST: COMPANYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("CompanyId,Name,Description,Phone,Email,Address,CreatedDate,IsActive")] Company company)
    {
        if (id != company.CompanyId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(company);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CompanyExists(company.CompanyId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(company);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.CompanyId == id);

        if (company == null)
        {
            return NotFound();
        }

        company.IsActive = true;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.CompanyId == id);

        if (company == null)
        {
            return NotFound();
        }

        company.IsActive = false;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    
    

    private bool CompanyExists(int? id)
    {
        return _context.Companies.Any(e => e.CompanyId == id);
    }
}
