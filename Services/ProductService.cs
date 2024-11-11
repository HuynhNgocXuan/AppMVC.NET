using webMVC.Models;

namespace webMVC.Services {

    public class ProductService : List<ProductModel> {
        public ProductService() {
            this.AddRange(new ProductModel[] {
                new ProductModel() { Id = 1, Name = "Product 1" },
                new ProductModel() { Id = 2, Name = "Product 2" },
                new ProductModel() { Id = 3, Name = "Product 3" },
            });
        }
    }
}

