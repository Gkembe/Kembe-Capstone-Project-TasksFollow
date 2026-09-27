using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        public ApplicationUser ApplicationUser { get; set; } = null!;

        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Joble Title")]
        public string JobTitle { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Department { get; set; }

        [Display(Name = "Added Date")]
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "The company is required.")]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();

        public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();

        public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();

    }
}
