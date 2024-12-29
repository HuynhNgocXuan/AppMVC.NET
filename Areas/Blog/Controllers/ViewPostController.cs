using webMVC.Models;
using webMVC.Models.Blog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace webMVC.Areas.Blog.Controllers
{
    [Area("Blog")]
    public class ViewPostController : Controller
    {
        private readonly ILogger<ViewPostController> _logger;
        private readonly AppDbContext _context;
 
        public ViewPostController(ILogger<ViewPostController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // /post/
        // /post/{categorySlug?}
        [Route("/post/{categorySlug?}")]
        public IActionResult Index(string categorySlug, [FromQuery(Name = "p")] int currentPage, int pagesize)
        {
            var categories = GetCategories();
            ViewBag.categories = categories;
            ViewBag.categorySlug = categorySlug;

            Category category = null!;

            if (!string.IsNullOrEmpty(categorySlug))
            {
                category = _context.Categories!.Where(c => c.Slug == categorySlug)
                                    .Include(c => c.CategoryChildren)
                                    .FirstOrDefault()!;

                if (category == null)
                {
                    return NotFound("Không thấy category");
                }
            }

            var posts = _context.Posts!
                                .Include(p => p.Author)
                                .Include(p => p.PostCategories!)
                                .ThenInclude(p => p.Category)
                                .AsQueryable();

            posts.OrderByDescending(p => p.DateUpdated);

            if (category != null)
            {
                var ids = new List<int>();
                category.ChildCategoryIDs(null!, ids);
                ids.Add(category.Id);

                posts = posts.Where(p => p.PostCategories!.Where(pc => ids.Contains(pc.CategoryID)).Any());
            }

            int totalPosts = posts.Count();
            if (pagesize <= 0) pagesize = 10;
            int countPages = (int)Math.Ceiling((double)totalPosts / pagesize);

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

            var postsInPage = posts.Skip((currentPage - 1) * pagesize)
                             .Take(pagesize);


            ViewBag.pagingModel = pagingModel;
            ViewBag.totalPosts = totalPosts;



            ViewBag.category = category; 
            return View(postsInPage.ToList());
        }

        [Route("/post/{postSlug}.html")]
        public IActionResult Detail(string postSlug)
        {
            var categories = GetCategories();
            ViewBag.categories = categories;

            var post = _context.Posts!.Where(p => p.Slug == postSlug)
                               .Include(p => p.Author)
                               .Include(p => p.PostCategories)!
                               .ThenInclude(pc => pc.Category)
                               .FirstOrDefault();

            if (post == null)
            {
                return NotFound("Không thấy bài viết");
            }

            Category category = post.PostCategories!.FirstOrDefault()?.Category!;
            ViewBag.category = category;

            var otherPosts = _context.Posts!.Where(p => p.PostCategories!.Any(c => c.Category!.Id == category!.Id))
                                            .Where(p => p.PostId != post.PostId)
                                            .OrderByDescending(p => p.DateUpdated)
                                            .Take(5);
            ViewBag.otherPosts = otherPosts;

            return View(post);
        }

        private List<Category> GetCategories()
        {
            var categories = _context.Categories!
                            .Include(c => c.CategoryChildren)
                            .AsEnumerable()
                            .Where(c => c.ParentCategory == null)
                            .ToList();
            return categories;
        }

    }
}