namespace TasksFollow.Models
{

    public class Payment
    {
        public int PaymentId { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public decimal AmountPaid { get; set; }

        public decimal MonthlyPrice { get; set; }

        public int MonthsPaid { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public DateTime CoverageStartDate { get; set; }

        public DateTime CoverageEndDate { get; set; }
    }
}