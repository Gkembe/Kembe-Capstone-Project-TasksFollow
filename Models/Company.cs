using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class Company
    {
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "You must add company's Name")]
        
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "You must add company's Phone Number")]
        [StringLength(14)]
        public string Phone { get; set;} = string.Empty;

        [Required(ErrorMessage = "You must add an email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "You must add an address")]
        public string? Address { get; set; }

        [Display(Name = "Create Date")]
        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

        public ICollection<Project> Projects { get; set; } = new List<Project>();

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();

        public ICollection<GroupChat> GroupChats { get; set; } = new List<GroupChat>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();


    }
}
