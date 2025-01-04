
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Identity;

namespace webMVC.Areas.Identity.Models.RoleViewModels
{
    public class RoleModel : IdentityRole
    {
        public string[]? Claims { get; set; }

    }
}
