namespace TasksFollow.Models
{
    public class AdminDashboardViewModel
    {
        // Companies
        public int TotalCompanies { get; set; }
        public int ActiveCompanies { get; set; }
        public int InactiveCompanies { get; set; }

        // Managers
        public int TotalManagers { get; set; }
        public int ActiveManagers { get; set; }
        public int InactiveManagers { get; set; }

        // Subscriptions
        public int ActiveSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int CompaniesWithoutSubscription { get; set; }

        // Payments
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int PaymentsThisMonth { get; set; }

        // Lists
        public List<Payment> RecentPayments { get; set; } = new List<Payment>();
        public List<Payment> ExpiringSubscriptions { get; set; } = new List<Payment>();
    }
}
