using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class ProjectMember
    {
        public int ProjectMemberId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public Project Project { get; set; } = null!;

        [Required]
        public int EmployeeId { get; set; }

        public Employee Employee { get; set; } = null!;

        [DataType(DataType.Date)]
        [Display(Name = "Added Date")]
        public DateTime AddedDate { get; set; } = DateTime.Now;
    }
}
