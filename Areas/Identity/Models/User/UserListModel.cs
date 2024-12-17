using System.Collections.Generic;
using webMVC.Models; 

namespace webMVC.Areas.Identity.Models.UserViewModels
{ 
    public class UserListModel
    {
        public int totalUsers { get; set; }
        
        public int countPages { get; set; }
 
        public int ITEMS_PER_PAGE { get; set; } = 1;

        public int currentPage { get; set; }

        public List<UserAndRole>? users { get; set; }

    }

    public class UserAndRole : AppUser
    {
        public string? RoleNames { get; set; }
    }
}