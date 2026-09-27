using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class GroupChat
    {
        public int GroupChatId { get; set; }

        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [Required]
        public int CompanyId { get; set; }

        public Company Company { get; set; } = null!;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
