using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models.ViewModels
{
    public class PaymentCreateViewModel
    {
        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please select a subscription plan.")]
        [Display(Name = "Subscription Plan")]
        public int? SubscriptionPlanId { get; set; }


        [Required(ErrorMessage = "Payment amount is required.")]
        [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "Payment amount must be greater than 0.")]
        [Display(Name = "Amount Paid")]
        public decimal? AmountPaid { get; set; }
    }
}