
using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        // Employee who receives the notification
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        // Notification information
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        // Related task
        public int? ProjectTaskId { get; set; }
        public ProjectTask? ProjectTask { get; set; }

        // Read status
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ReadDate { get; set; }
    }
}
