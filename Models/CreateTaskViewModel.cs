
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TasksFollow.Models
{
    public class CreateTaskViewModel
    {
        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a project.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a project.")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "Not Started";

        [Display(Name = "Priority")]
        public string? Priority { get; set; } = "Medium";

        [Range(0, 100)]
        [Display(Name = "Progress (%)")]
        public int Percent { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        
        public List<SelectListItem>? Projects { get; set; } = new List<SelectListItem>();
    }
}
