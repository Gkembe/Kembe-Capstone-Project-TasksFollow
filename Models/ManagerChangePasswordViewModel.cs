
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class ManagerChangePasswordViewModel
    {
        [Required (ErrorMessage = "Current Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "New Password is required.")]
        [DataType(DataType.Password)]
        [Display (Name ="New Password")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
    }
}
