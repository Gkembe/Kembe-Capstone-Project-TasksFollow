namespace TasksFollow.Models
{
    public class ManagerIndexViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string MiddleName {  get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int? CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;
    }
}
