
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TasksFollow.Models
{
    public class CreateGroupViewModel
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public List<int> SelectedEmployeeIds { get; set; } = new List<int>();

        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
    }
}
