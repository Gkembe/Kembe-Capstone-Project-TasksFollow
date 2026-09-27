using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TasksFollow.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        [Required(ErrorMessage = "Message cannot be empty.")]
        [StringLength(3000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentDate { get; set; } = DateTime.Now;

        public DateTime? EditedDate { get; set; }

        public bool IsDeleted { get; set; }

        [Required]
        public int GroupChatId { get; set; }

        public GroupChat GroupChat { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public int? ParentMessageId { get; set; }

        public Message? ParentMessage { get; set; }

        public ICollection<Message> Replies { get; set; } = new List<Message>();
    }
}
