
using webMVC.Models.Blog;
using Microsoft.AspNetCore.Mvc;

namespace webMVC.Components
{
    [ViewComponent]
    public class CategorySidebar : ViewComponent
    {

        public class CategorySidebarData
        {
            public List<Category>? Categories { get; set; }
            public int level { get; set; }

            public string? categorySlug { get; set; }

        }

        public IViewComponentResult Invoke(CategorySidebarData data)
        {
            return View(data);
        }

    }
}