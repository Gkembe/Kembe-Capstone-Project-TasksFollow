using System.ComponentModel.DataAnnotations;

namespace TasksFollow.Models
{
    public class ReportViewModel
    {
        // Filters
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public int? CompanyId { get; set; }
        public int? SubscriptionPlanId { get; set; }

        // Summary
        public decimal TotalRevenue { get; set; }
        public int TotalPayments { get; set; }
        public int CompaniesPaid { get; set; }

        // Report data
        public List<Payment> Payments { get; set; } = new List<Payment>();
    }
}