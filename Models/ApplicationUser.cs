using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace TasksFollow.Models;
// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Middle Name")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;
    public int? CompanyID { get; set; }

    public Company? Company { get; set; }

    public Employee? Employee { get; set; }
}
