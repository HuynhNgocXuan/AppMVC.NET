using Microsoft.AspNetCore.Mvc;
using webMVC.Services;

namespace webMVC.Controllers
{
    
    [Area("ProductManager")]
    [Route("sanpham")]
    public class ProductController : Controller
    {
        private readonly ILogger _logger;
        private readonly ProductService _productService;

        public ProductController(ILogger<ProductController> logger,  ProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }


        // GET: Product
        public ActionResult Index()
        {
            var products = _productService.OrderBy(p => p.Name).ToList();
            return View(products);
        }

    }
}
