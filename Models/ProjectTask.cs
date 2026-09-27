using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class ProjectTask
    {
        public int ProjectTaskId { get; set; }

        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(200, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Not Started";

        [StringLength(30)]
        public string? Priority { get; set; }

        [Range(0, 100)]
        public int Percent { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
