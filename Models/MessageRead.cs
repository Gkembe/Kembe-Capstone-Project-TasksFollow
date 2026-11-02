
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class MessageRead
    {
        public int MessageReadId { get; set; }

        [Required]
        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public DateTime ReadDate { get; set; } = DateTime.Now;
    }
}
