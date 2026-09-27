using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace TasksFollow.Models
{
    public class TaskAssignment
    {
        public int TaskAssignmentId { get; set; }

        public int ProjectTaskId { get; set; }

        public ProjectTask ProjectTask { get; set; } = null!;

        [Required]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        [Required]
        public string AssignedByUserId { get; set; } = string.Empty;
        public ApplicationUser AssignedByUser { get; set; } = null!;

        [Display(Name = "Assigned Date")]
        [DataType(DataType.Date)]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Display(Name = "Completed Date")]
        [DataType(DataType.Date)]
        public DateTime? CompletDate { get; set; }

        public bool IsCompleted { get; set; } = false;




    }
}
