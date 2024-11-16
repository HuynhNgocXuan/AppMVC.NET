using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

namespace webMVC.Models.Contact {
    public class ContactModel {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DisplayName("Họ và tên")]
        [Required(ErrorMessage = "Phải nhập {0} người dùng")]
        [StringLength(100)]
        [Column(TypeName = "nvarchar")]
        public string Name { get; set; }


        [DisplayName("Địa chỉ email")]
        [Required(ErrorMessage = "Phải đúng cấu trúc địa chỉ {0}")]
        [Column(TypeName = "nvarchar")]
        [StringLength(50)]
        [EmailAddress(ErrorMessage = "Phải là địa chỉ email")]
        public string Email { get; set; }
        

        [DisplayName("Ngày gửi")]
        [Column(TypeName = "datetime")]
        public DateTime DateTimeSend{ get; set; } = DateTime.Now;

       

        [Required(ErrorMessage = "Phải nhập {0}")]
        [DisplayName("Nội dung")]
        [Column(TypeName = "text")]
        public string Message { get; set; } 

        [Required(ErrorMessage = "Phải nhập {0}")]
        [StringLength(50)]
        [Phone(ErrorMessage = "Phải là số diện thoại")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }
    }
}