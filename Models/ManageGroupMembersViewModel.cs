using Microsoft.AspNetCore.Mvc.Rendering;

namespace TasksFollow.Models
{
    public class ManageGroupMembersViewModel
    {
        public int GroupChatId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<int> SelectedEmployeeIds { get; set; } = new List<int>();
        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
    }
}