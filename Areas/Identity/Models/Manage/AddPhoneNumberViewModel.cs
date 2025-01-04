
using System.ComponentModel.DataAnnotations;


namespace webMVC.Areas.Identity.Models.ManageViewModels
{
    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }
    }
}
