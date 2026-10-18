
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class EditTaskViewModel
    {
        public int ProjectTaskId { get; set; }

        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(200, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public string Status { get; set; } = "Not Started";

        public string? Priority { get; set; }

        [Range(0, 100)]
        [Display(Name = "Progress (%)")]
        public int Percent { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }
    }
}
