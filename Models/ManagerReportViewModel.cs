
namespace TasksFollow.Models
{
    public class ManagerReportViewModel
    {
        // Summary statistics
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TotalEmployees { get; set; }

        // Task completion percentage
        public double CompletionRate { get; set; }

        // Project performance
        public List<ProjectReportItem> Projects { get; set; } = new List<ProjectReportItem>();
    }

    public class ProjectReportItem
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double CompletionRate { get; set; }
    }
}
