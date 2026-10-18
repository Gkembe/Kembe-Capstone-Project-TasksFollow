
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class CreateProjectViewModel
    {
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(200)]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Required(ErrorMessage = "Project status is required.")]
        public string Status { get; set; } = "Not Started";
    }
}
