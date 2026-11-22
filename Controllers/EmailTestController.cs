
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasksFollow.Services;

namespace TasksFollow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmailTestController : Controller
    {
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public EmailTestController(EmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTest()
        {
            string? email = _configuration["Resend:TestEmail"];

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Test email is not configured.";
                return RedirectToAction("Index");
            }

            try
            {
                await _emailService.SendEmailAsync(
                    email,
                    "TasksFollow - Email Test",
                    "Hello! This is a test email from TasksFollow."
                );

                TempData["SuccessMessage"] = "Email request submitted successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Unable to send email. Check Resend logs.";
            }

            return RedirectToAction("Index");
        }
    }
}
