using System.ComponentModel.DataAnnotations.Schema;

namespace webMVC.Models.Product
{
    [Table("CategoryAndProduct")]
    public class CategoryAndProduct
    {
        public int ProductID { set; get; }

        public int CategoryID { set; get; }


        [ForeignKey("ProductID")]
        public ProductModel? Product { set; get; }


        [ForeignKey("CategoryID")]
        public CategoryProduct? Category { set; get; }
    }
}