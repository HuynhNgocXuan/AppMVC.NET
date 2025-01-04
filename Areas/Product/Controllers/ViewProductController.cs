using webMVC.Models;
using webMVC.Models.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webMVC.Areas.Product.Models;

namespace AppMvc.Net.Areas.Product.Controllers
{
    [Area("Product")]
    public class ViewProductController : Controller
    {
        private readonly ILogger<ViewProductController> _logger;
        private readonly AppDbContext _context;

        private readonly CartService _cartService;

        public ViewProductController(ILogger<ViewProductController> logger, AppDbContext context, CartService cartService)
        {
            _logger = logger;
            _context = context;
            _cartService = cartService;
        }

        // /post/
        // /post/{categorySlug?}
        [Route("/products/{categorySlug?}")]
        public IActionResult Index(string categorySlug, [FromQuery(Name = "p")] int currentPage, int pagesize)
        {
            var categories = GetCategories();

            ViewBag.categories = categories;
            ViewBag.categorySlug = categorySlug;

            CategoryProduct? category = null;

            if (!string.IsNullOrEmpty(categorySlug))
            {
                category = _context.CategoryProducts!.Where(c => c.Slug == categorySlug)
                                    .Include(c => c.CategoryChildren)
                                    .FirstOrDefault()!;

                if (category == null)
                {
                    return NotFound("Không thấy category");
                }
            }

            var products = _context.Products!
                                .Include(p => p.Author)
                                .Include(p => p.Photos)
                                .Include(p => p.CategoryAndProducts)!
                                .ThenInclude(p => p.Category)
                                .AsQueryable();

            products = products.OrderByDescending(p => p.DateUpdated);

            if (category != null)
            {
                var ids = new List<int>();
                category.ChildCategoryIDs(null!, ids);
                ids.Add(category.Id);


                products = products.Where(p => p.CategoryAndProducts!.Where(pc => ids.Contains(pc.CategoryID)).Any());


            }

            int totalProducts = products.Count();
            if (pagesize <= 0) pagesize = 20;
            int countPages = (int)Math.Ceiling((double)totalProducts / pagesize);

            if (currentPage > countPages) currentPage = countPages;
            if (currentPage < 1) currentPage = 1;

            var pagingModel = new PagingModel()
            {
                countPages = countPages,
                currentPage = currentPage,
                generateUrl = (pageNumber) => Url.Action("Index", new
                {
                    p = pageNumber,
                    pagesize = pagesize
                }) ?? string.Empty
            };

            var productsInPage = products.Skip((currentPage - 1) * pagesize)
                             .Take(pagesize);


            ViewBag.pagingModel = pagingModel;
            ViewBag.totalPosts = totalProducts;



            ViewBag.category = category;
            return View(productsInPage.ToList());
        }

        [Route("/product/{productSlug}.html")]
        public IActionResult Detail(string productSlug)
        {
            var categories = GetCategories();
            ViewBag.categories = categories;

            var product = _context.Products!.Where(p => p.Slug == productSlug)
                               .Include(p => p.Author)
                               .Include(p => p.Photos)
                               .Include(p => p.CategoryAndProducts)!
                               .ThenInclude(pc => pc.Category) // Ghi chu . cham de coi Category hay la ProductCategory 
                               .FirstOrDefault();

            if (product == null)
            {
                return NotFound("Không thấy bài viết");
            }

            CategoryProduct category = product.CategoryAndProducts!.FirstOrDefault()?.Category!;
            ViewBag.category = category;

            var otherProducts = _context.Products!.Where(p => p.CategoryAndProducts!.Any(c => c.Category!.Id == category.Id))
                                            .Where(p => p.ProductID != product.ProductID)
                                            .OrderByDescending(p => p.DateUpdated)
                                            .Take(5);
            ViewBag.otherProducts = otherProducts;

            return View(product);
        }

        private List<CategoryProduct> GetCategories()
        {
            var categories = _context.CategoryProducts!
                            .Include(c => c.CategoryChildren)
                            .AsEnumerable()
                            .Where(c => c.ParentCategory == null)
                            .ToList();
            return categories;
        }

        [Route("/cart", Name = "cart")]
        public IActionResult Cart()
        {
            return View(_cartService.GetCartItems());
        }


        [Route("add-cart/{productId:int}", Name = "add-cart")]
        public IActionResult AddToCart([FromRoute] int productId)
        {

            var product = _context.Products!
                .Where(p => p.ProductID == productId)
                .FirstOrDefault();
            if (product == null)
                return NotFound("Không có sản phẩm");


            var cart = _cartService.GetCartItems();
            var cartItem = cart.Find(p => p.product.ProductID == productId);
            if (cartItem != null)
            {
                cartItem.quantity++;
            }
            else
            {
                cart.Add(new CartItem() { quantity = 1, product = product });
            }


            _cartService.SaveCartSession(cart);

            return RedirectToAction(nameof(Cart));
        }





        [Route("/remove-cart/{productId:int}", Name = "removeCart")]
        public IActionResult RemoveCart([FromRoute] int productId)
        {
            var cart = _cartService.GetCartItems();
            var cartItem = cart.Find(p => p.product.ProductID == productId);
            if (cartItem != null)
            {

                cart.Remove(cartItem);
            }

            _cartService.SaveCartSession(cart);
            return RedirectToAction(nameof(Cart));
        }


        [Route("/update-cart", Name = "updateCart")]
        [HttpPost]
        public IActionResult UpdateCart([FromForm] int productId, [FromForm] int quantity)
        {

            var cart = _cartService.GetCartItems();
            var cartItem = cart.Find(p => p.product.ProductID == productId);
            if (cartItem != null)
            {

                cartItem.quantity = quantity;
            }
            _cartService.SaveCartSession(cart);

            return Ok();
        }

        [Route("/checkout")]
        public IActionResult Checkout()
        {
            var cart = _cartService.GetCartItems();


            _cartService.ClearCart();

            return Content("Da gui don hang");

        }

    }
}