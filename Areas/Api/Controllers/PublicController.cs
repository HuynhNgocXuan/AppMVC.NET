using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webMVC.Models;
using webMVC.Models.Product;
using webMVC.Models.Blog;
using webMVC.Data;

namespace webMVC.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    public class PublicController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PublicController> _logger;

        public PublicController(AppDbContext context, ILogger<PublicController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/public/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<object>>> GetDashboardData()
        {
            try
            {
                var totalProducts = await _context.Products!.CountAsync();
                var totalBlogs = await _context.Posts!.Where(p => p.Published).CountAsync();
                var totalCategories = await _context.CategoryProducts!.CountAsync();
                var totalUsers = await _context.Users.CountAsync();

                var recentProducts = await _context.Products!
                    .Include(p => p.Author)
                    .Include(p => p.Photos)
                    .OrderByDescending(p => p.DateUpdated)
                    .Take(5)
                    .Select(p => new
                    {
                        p.ProductID,
                        p.Title,
                        p.Price,
                        p.DateUpdated,
                        AuthorName = p.Author!.UserName,
                        PhotoCount = p.Photos!.Count
                    })
                    .ToListAsync();

                var recentBlogs = await _context.Posts!
                    .Include(p => p.Author)
                    .Where(p => p.Published)
                    .OrderByDescending(p => p.DateUpdated)
                    .Take(5)
                    .Select(p => new
                    {
                        p.PostId,
                        p.Title,
                        p.DateUpdated,
                        AuthorName = p.Author!.UserName
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Dashboard data retrieved successfully",
                    Data = new
                    {
                        Statistics = new
                        {
                            TotalProducts = totalProducts,
                            TotalBlogs = totalBlogs,
                            TotalCategories = totalCategories,
                            TotalUsers = totalUsers
                        },
                        RecentProducts = recentProducts,
                        RecentBlogs = recentBlogs
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/public/featured-products
        [HttpGet("featured-products")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetFeaturedProducts([FromQuery] int count = 8)
        {
            try
            {
                var products = await _context.Products!
                    .Include(p => p.Author)
                    .Include(p => p.Photos)
                    .Include(p => p.CategoryAndProducts!)
                    .ThenInclude(pc => pc.Category)
                    .Where(p => p.Published)
                    .OrderByDescending(p => p.DateUpdated)
                    .Take(count)
                    .Select(p => new
                    {
                        p.ProductID,
                        p.Title,
                        p.Description,
                        p.Price,
                        p.Slug,
                        p.DateUpdated,
                        AuthorName = p.Author!.UserName,
                        Categories = p.CategoryAndProducts!.Select(pc => pc.Category!.Title).ToList(),
                        MainPhoto = p.Photos!.FirstOrDefault() != null ? $"/contents/Products/{p.Photos!.First().FileName}" : null
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Featured products retrieved successfully",
                    Data = products.Cast<object>().ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured products");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/public/latest-blogs
        [HttpGet("latest-blogs")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetLatestBlogs([FromQuery] int count = 6)
        {
            try
            {
                var blogs = await _context.Posts!
                    .Include(p => p.Author)
                    .Include(p => p.PostCategories!)
                    .ThenInclude(pc => pc.Category)
                    .Where(p => p.Published)
                    .OrderByDescending(p => p.DateUpdated)
                    .Take(count)
                    .Select(p => new
                    {
                        p.PostId,
                        p.Title,
                        p.Description,
                        p.Slug,
                        p.DateUpdated,
                        AuthorName = p.Author!.UserName,
                        Categories = p.PostCategories!.Select(pc => pc.Category!.Title).ToList()
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Latest blogs retrieved successfully",
                    Data = blogs.Cast<object>().ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest blogs");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/public/categories
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetCategories()
        {
            try
            {
                var productCategories = await _context.CategoryProducts!
                    .OrderBy(c => c.Title)
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.Slug,
                        Type = "Product"
                    })
                    .ToListAsync();

                var blogCategories = await _context.Categories!
                    .OrderBy(c => c.Title)
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.Slug,
                        Type = "Blog"
                    })
                    .ToListAsync();

                var allCategories = productCategories.Cast<object>().Concat(blogCategories.Cast<object>()).ToList();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Categories retrieved successfully",
                    Data = allCategories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/public/products-by-category/{categoryId}
        [HttpGet("products-by-category/{categoryId}")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetProductsByCategory(int categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 12)
        {
            try
            {
                var query = _context.Products!
                    .Include(p => p.Author)
                    .Include(p => p.Photos)
                    .Include(p => p.CategoryAndProducts!)
                    .ThenInclude(pc => pc.Category)
                    .Where(p => p.Published && p.CategoryAndProducts!.Any(pc => pc.CategoryID == categoryId))
                    .AsQueryable();

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var products = await query
                    .OrderByDescending(p => p.DateUpdated)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.ProductID,
                        p.Title,
                        p.Description,
                        p.Price,
                        p.Slug,
                        p.DateUpdated,
                        AuthorName = p.Author!.UserName,
                        Categories = p.CategoryAndProducts!.Select(pc => pc.Category!.Title).ToList(),
                        MainPhoto = p.Photos!.FirstOrDefault() != null ? $"/contents/Products/{p.Photos!.First().FileName}" : null
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Products by category retrieved successfully",
                    Data = products.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by category {CategoryId}", categoryId);
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/public/blogs-by-category/{categoryId}
        [HttpGet("blogs-by-category/{categoryId}")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetBlogsByCategory(int categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Posts!
                    .Include(p => p.Author)
                    .Include(p => p.PostCategories!)
                    .ThenInclude(pc => pc.Category)
                    .Where(p => p.Published && p.PostCategories!.Any(pc => pc.CategoryID == categoryId))
                    .AsQueryable();

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var blogs = await query
                    .OrderByDescending(p => p.DateUpdated)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.PostId,
                        p.Title,
                        p.Description,
                        p.Slug,
                        p.DateUpdated,
                        AuthorName = p.Author!.UserName,
                        Categories = p.PostCategories!.Select(pc => pc.Category!.Title).ToList()
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Blogs by category retrieved successfully",
                    Data = blogs.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blogs by category {CategoryId}", categoryId);
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/public/search
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<object>>> Search([FromQuery] string q, [FromQuery] string? type = "all")
        {
            try
            {
                if (string.IsNullOrEmpty(q))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Search term is required"
                    });
                }

                var results = new Dictionary<string, object>();

                if (type == "all" || type == "products")
                {
                    var products = await _context.Products!
                        .Include(p => p.Author)
                        .Include(p => p.Photos)
                        .Where(p => p.Published && (p.Title!.Contains(q) || p.Description!.Contains(q)))
                        .OrderByDescending(p => p.DateUpdated)
                        .Take(5)
                        .Select(p => new
                        {
                            p.ProductID,
                            p.Title,
                            p.Price,
                            p.Slug,
                            Type = "Product",
                            MainPhoto = p.Photos!.FirstOrDefault() != null ? $"/contents/Products/{p.Photos!.First().FileName}" : null
                        })
                        .ToListAsync();

                    results["products"] = products;
                }

                if (type == "all" || type == "blogs")
                {
                    var blogs = await _context.Posts!
                        .Include(p => p.Author)
                        .Where(p => p.Published && (p.Title!.Contains(q) || p.Description!.Contains(q)))
                        .OrderByDescending(p => p.DateUpdated)
                        .Take(5)
                        .Select(p => new
                        {
                            p.PostId,
                            p.Title,
                            p.Slug,
                            Type = "Blog"
                        })
                        .ToListAsync();

                    results["blogs"] = blogs;
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Search results for '{q}'",
                    Data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
}
