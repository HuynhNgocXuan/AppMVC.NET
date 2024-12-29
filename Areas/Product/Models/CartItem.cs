using webMVC.Models.Product;

namespace webMVC.Areas.Product.Models
{
    public class CartItem
    {
        public required int quantity { set; get; }
        public required ProductModel product { set; get; }
    }
}