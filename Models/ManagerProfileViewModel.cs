
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class ManagerProfileViewModel
    {
        [Required (ErrorMessage ="Last Name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }
    }
}
