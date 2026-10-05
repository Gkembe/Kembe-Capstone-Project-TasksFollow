using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class SubscriptionPlan
    {
        public int SubscriptionPlanId { get; set; }

        [Required (ErrorMessage = "Plan name required.")]
        [StringLength(50)]
        [Display (Name = "Plan Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Monthly price is required.")]
        [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "Monthly price must be greater than 0.")]
        [Display(Name = "Monthly Price")]
        public decimal? MonthlyPrice { get; set; }

        public bool IsActive { get; set; } = true;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
