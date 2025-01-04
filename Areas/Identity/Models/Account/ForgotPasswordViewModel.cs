using System.ComponentModel.DataAnnotations;

namespace webMVC.Areas.Identity.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "Phải là địa chỉ email đã đăng nhập")]
        public string? Email { get; set; }
    }
}
