using System.ComponentModel.DataAnnotations;
using webMVC.Models.Product;

namespace webMVC.Areas.Product.Models
{
    public class CreateProductModel : ProductModel
    {
        [Display(Name = "Chuyên mục")]
        public int[]? CategoryIDs { get; set; }
    }
}
