
using System.ComponentModel.DataAnnotations;


namespace webMVC.Areas.Identity.Models.ManageViewModels
{
    public class RemoveLoginViewModel
    {
        public string? LoginProvider { get; set; }
        public string? ProviderKey { get; set; }
    }
}
