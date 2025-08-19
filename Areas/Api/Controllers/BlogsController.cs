using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webMVC.Models;
using webMVC.Models.Blog;
using webMVC.Data;
using webMVC.Areas.Blog.Models;
using webMVC.Utilities;
using System.Linq.Dynamic.Core;

namespace webMVC.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BlogsController> _logger;

        public BlogsController(AppDbContext context, ILogger<BlogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/blogs
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Post>>>> GetBlogs([FromQuery] ApiPaginationRequest request)
        {
            try
            {
                var query = _context.Posts!
                    .Include(p => p.Author)
                    .Include(p => p.PostCategories!)
                    .ThenInclude(pc => pc.Category)
                    .Where(p => p.Published)
                    .AsQueryable();

                // Search
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(p => 
                        p.Title!.Contains(request.SearchTerm) || 
                        p.Description!.Contains(request.SearchTerm) ||
                        p.Content!.Contains(request.SearchTerm));
                }

                // Sort
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    var sortOrder = request.SortOrder?.ToLower() == "desc" ? "descending" : "ascending";
                    query = query.OrderBy($"{request.SortBy} {sortOrder}");
                }
                else
                {
                    query = query.OrderByDescending(p => p.DateUpdated);
                }

                // Pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
                var posts = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return Ok(new ApiResponse<List<Post>>
                {
                    Success = true,
                    Message = "Blogs retrieved successfully",
                    Data = posts,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blogs");
                return StatusCode(500, new ApiResponse<List<Post>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/blogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Post>>> GetBlog(int id)
        {
            try
            {
                var post = await _context.Posts!
                    .Include(p => p.Author)
                    .Include(p => p.PostCategories!)
                    .ThenInclude(pc => pc.Category)
                    .FirstOrDefaultAsync(p => p.PostId == id && p.Published);

                if (post == null)
                {
                    return NotFound(new ApiResponse<Post>
                    {
                        Success = false,
                        Message = "Blog not found"
                    });
                }

                return Ok(new ApiResponse<Post>
                {
                    Success = true,
                    Message = "Blog retrieved successfully",
                    Data = post
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blog {BlogId}", id);
                return StatusCode(500, new ApiResponse<Post>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // POST: api/blogs
        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<ApiResponse<Post>>> CreateBlog([FromBody] CreatePostModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<Post>
                    {
                        Success = false,
                        Message = "Invalid model data",
                        Data = null
                    });
                }

                var post = new Post
                {
                    Title = model.Title,
                    Description = model.Description,
                    Slug = model.Slug ?? AppUtilities.GenerateSlug(model.Title!),
                    Content = model.Content,
                    Published = model.Published,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow
                };

                // Get current user
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    post.AuthorId = userId;
                }

                _context.Posts!.Add(post);
                await _context.SaveChangesAsync();

                // Add categories
                if (model.CategoryIDs != null && model.CategoryIDs.Any())
                {
                    foreach (var categoryId in model.CategoryIDs)
                    {
                        _context.Add(new PostCategory
                        {
                            PostID = post.PostId,
                            CategoryID = categoryId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // Reload with includes
                var createdPost = await _context.Posts!
                    .Include(p => p.Author)
                    .Include(p => p.PostCategories!)
                    .ThenInclude(pc => pc.Category)
                    .FirstOrDefaultAsync(p => p.PostId == post.PostId);

                return CreatedAtAction(nameof(GetBlog), new { id = post.PostId }, new ApiResponse<Post>
                {
                    Success = true,
                    Message = "Blog created successfully",
                    Data = createdPost
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog");
                return StatusCode(500, new ApiResponse<Post>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // PUT: api/blogs/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<ApiResponse<Post>>> UpdateBlog(int id, [FromBody] CreatePostModel model)
        {
            try
            {
                var post = await _context.Posts!.FindAsync(id);
                if (post == null)
                {
                    return NotFound(new ApiResponse<Post>
                    {
                        Success = false,
                        Message = "Blog not found"
                    });
                }

                post.Title = model.Title;
                post.Description = model.Description;
                post.Slug = model.Slug ?? AppUtilities.GenerateSlug(model.Title!);
                post.Content = model.Content;
                post.Published = model.Published;
                post.DateUpdated = DateTime.UtcNow;

                // Update categories
                var existingCategories = await _context.PostCategories!
                    .Where(pc => pc.PostID == id)
                    .ToListAsync();
                _context.PostCategories!.RemoveRange(existingCategories);

                if (model.CategoryIDs != null && model.CategoryIDs.Any())
                {
                    foreach (var categoryId in model.CategoryIDs)
                    {
                        _context.Add(new PostCategory
                        {
                            PostID = post.PostId,
                            CategoryID = categoryId
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Reload with includes
                var updatedPost = await _context.Posts!
                    .Include(p => p.Author)
                    .Include(p => p.PostCategories!)
                    .ThenInclude(pc => pc.Category)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                return Ok(new ApiResponse<Post>
                {
                    Success = true,
                    Message = "Blog updated successfully",
                    Data = updatedPost
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog {BlogId}", id);
                return StatusCode(500, new ApiResponse<Post>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // DELETE: api/blogs/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteBlog(int id)
        {
            try
            {
                var post = await _context.Posts!.FindAsync(id);
                if (post == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Blog not found"
                    });
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Blog deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog {BlogId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/blogs/search
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<Post>>>> SearchBlogs([FromQuery] string q, [FromQuery] ApiPaginationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(q))
                {
                    return BadRequest(new ApiResponse<List<Post>>
                    {
                        Success = false,
                        Message = "Search term is required"
                    });
                }

                var query = _context.Posts!
                    .Include(p => p.Author)
                    .Include(p => p.PostCategories!)
                    .ThenInclude(pc => pc.Category)
                    .Where(p => p.Published)
                    .Where(p => 
                        p.Title!.Contains(q) || 
                        p.Description!.Contains(q) ||
                        p.Content!.Contains(q))
                    .AsQueryable();

                // Sort
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    var sortOrder = request.SortOrder?.ToLower() == "desc" ? "descending" : "ascending";
                    query = query.OrderBy($"{request.SortBy} {sortOrder}");
                }
                else
                {
                    query = query.OrderByDescending(p => p.DateUpdated);
                }

                // Pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
                var posts = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return Ok(new ApiResponse<List<Post>>
                {
                    Success = true,
                    Message = $"Found {totalCount} blogs matching '{q}'",
                    Data = posts,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching blogs");
                return StatusCode(500, new ApiResponse<List<Post>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/blogs/categories
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<Category>>>> GetCategories()
        {
            try
            {
                var categories = await _context.Categories!
                    .OrderBy(c => c.Title)
                    .ToListAsync();

                return Ok(new ApiResponse<List<Category>>
                {
                    Success = true,
                    Message = "Categories retrieved successfully",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new ApiResponse<List<Category>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
}
