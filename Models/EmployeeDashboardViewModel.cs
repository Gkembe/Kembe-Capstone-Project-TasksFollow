namespace TasksFollow.Models
{
    public class EmployeeDashboardViewModel
    {
       public int TotalAssignedTasks { get; set; }

       public int TotalInProgressTasks { get; set; }

       public int TotalCompletedTasks { get; set; }

       public int OverDueTasks { get; set; }

       public string CompanyName { get; set; }
    }
}
