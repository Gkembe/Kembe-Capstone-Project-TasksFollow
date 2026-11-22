
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class AdminResetPasswordViewModel
    {
        [Required]
        public string ManagerId { get; set; } = "";

        public string ManagerName { get; set; } = "";

       
    }
}
