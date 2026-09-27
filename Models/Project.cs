using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class Project
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(200)]
        [Display(Name = "Project Name ")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }


        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Not Started";

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

        public ICollection<ProjectTask> ProjectTask { get; set; } = new List<ProjectTask>();
    }
}
