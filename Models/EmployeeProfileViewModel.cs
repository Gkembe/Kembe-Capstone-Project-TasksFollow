
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class EmployeeProfileViewModel
    {
        [Required]
        public string FirstName { get; set; } = "";

        public string? MiddleName { get; set; }

        [Required]
        public string LastName { get; set; } = "";

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
