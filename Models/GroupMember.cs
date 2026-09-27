using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class GroupMember
    {
        public int GroupMemberId { get; set; }

        [Required]
        public int GroupChatId { get; set; }

        public GroupChat GroupChat { get; set; } = null!;

        [Required]
        public int EmployeeId { get; set; }

        public Employee Employee { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime JoinedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
