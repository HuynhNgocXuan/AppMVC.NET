using System.ComponentModel.DataAnnotations;


namespace webMVC.Areas.Identity.Models.AccountViewModels
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required(ErrorMessage = "Phải nhập {0}")]
        [EmailAddress(ErrorMessage = "Phải đúng định dạng email")]
        public string? Email { get; set; }
    }
}
