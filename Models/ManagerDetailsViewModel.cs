namespace TasksFollow.Models
{
    public class ManagerDetailsViewModel
    {
        public string Id { get; set; } = "";

        public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }

        public int? CompanyId { get; set; }
        public string CompanyName { get; set; } = "";

        public bool CompanyIsActive { get; set; }
    }
}
