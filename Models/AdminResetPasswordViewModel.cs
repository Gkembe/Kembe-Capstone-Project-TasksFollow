
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class AdminResetPasswordViewModel
    {
        [Required]
        public string ManagerId { get; set; } = "";

        public string ManagerName { get; set; } = "";

        [Required(ErrorMessage = "New Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
    }
}
