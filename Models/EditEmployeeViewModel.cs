
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class EditEmployeeViewModel
    {
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Job title is required.")]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; } = string.Empty;

        public string? Department { get; set; }
    }
}
