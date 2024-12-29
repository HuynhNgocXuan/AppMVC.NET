
using webMVC.Models.Product;
using Microsoft.AspNetCore.Mvc;

namespace webMVC.Components
{
    [ViewComponent]
    public class CategoryProductSidebar : ViewComponent
    {

        public class CategorySidebarData
        {
            public List<CategoryProduct>? Categories { get; set; }
            public int level { get; set; }

            public string? categorySlug { get; set; }

        }

        public IViewComponentResult Invoke(CategorySidebarData data)
        {
            return View(data);
        }

    }
}