using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webMVC.Models;
using webMVC.Models.Product;
using webMVC.Data;
using webMVC.Areas.Product.Models;
using webMVC.Utilities;
using System.Linq.Dynamic.Core;

namespace webMVC.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ProductModel>>>> GetProducts([FromQuery] ApiPaginationRequest request)
        {
            try
            {
                var query = _context.Products!
                    .Include(p => p.Author)
                    .Include(p => p.Photos)
                    .Include(p => p.CategoryAndProducts!)
                    .ThenInclude(pc => pc.Category)
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
                var products = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return Ok(new ApiResponse<List<ProductModel>>
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = products,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new ApiResponse<List<ProductModel>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductModel>>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products!
                    .Include(p => p.Author)
                    .Include(p => p.Photos)
                    .Include(p => p.CategoryAndProducts!)
                    .ThenInclude(pc => pc.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductModel>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<ProductModel>
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductModel>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // POST: api/products
        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<ApiResponse<ProductModel>>> CreateProduct([FromBody] CreateProductModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductModel>
                    {
                        Success = false,
                        Message = "Invalid model data",
                        Data = null
                    });
                }

                var product = new ProductModel
                {
                    Title = model.Title,
                    Description = model.Description,
                    Slug = model.Slug ?? AppUtilities.GenerateSlug(model.Title!),
                    Content = model.Content,
                    Published = model.Published,
                    Price = model.Price,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow
                };

                // Get current user
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    product.AuthorId = userId;
                }

                _context.Products!.Add(product);
                await _context.SaveChangesAsync();

                // Add categories
                if (model.CategoryIDs != null && model.CategoryIDs.Any())
                {
                    foreach (var categoryId in model.CategoryIDs)
                    {
                        _context.Add(new CategoryAndProduct
                        {
                            ProductID = product.ProductID,
                            CategoryID = categoryId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // Reload with includes
                var createdProduct = await _context.Products!
                    .Include(p => p.Author)
                    .Include(p => p.CategoryAndProducts!)
                    .ThenInclude(pc => pc.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == product.ProductID);

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductID }, new ApiResponse<ProductModel>
                {
                    Success = true,
                    Message = "Product created successfully",
                    Data = createdProduct
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new ApiResponse<ProductModel>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<ApiResponse<ProductModel>>> UpdateProduct(int id, [FromBody] CreateProductModel model)
        {
            try
            {
                var product = await _context.Products!.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductModel>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                product.Title = model.Title;
                product.Description = model.Description;
                product.Slug = model.Slug ?? AppUtilities.GenerateSlug(model.Title!);
                product.Content = model.Content;
                product.Published = model.Published;
                product.Price = model.Price;
                product.DateUpdated = DateTime.UtcNow;

                // Update categories
                var existingCategories = await _context.CategoryAndProducts!
                    .Where(cp => cp.ProductID == id)
                    .ToListAsync();
                _context.CategoryAndProducts!.RemoveRange(existingCategories);

                if (model.CategoryIDs != null && model.CategoryIDs.Any())
                {
                    foreach (var categoryId in model.CategoryIDs)
                    {
                        _context.Add(new CategoryAndProduct
                        {
                            ProductID = product.ProductID,
                            CategoryID = categoryId
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Reload with includes
                var updatedProduct = await _context.Products!
                    .Include(p => p.Author)
                    .Include(p => p.CategoryAndProducts!)
                    .ThenInclude(pc => pc.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                return Ok(new ApiResponse<ProductModel>
                {
                    Success = true,
                    Message = "Product updated successfully",
                    Data = updatedProduct
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductModel>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products!.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Product deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/products/search
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<ProductModel>>>> SearchProducts([FromQuery] string q, [FromQuery] ApiPaginationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(q))
                {
                    return BadRequest(new ApiResponse<List<ProductModel>>
                    {
                        Success = false,
                        Message = "Search term is required"
                    });
                }

                var query = _context.Products!
                    .Include(p => p.Author)
                    .Include(p => p.Photos)
                    .Include(p => p.CategoryAndProducts!)
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
                var products = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return Ok(new ApiResponse<List<ProductModel>>
                {
                    Success = true,
                    Message = $"Found {totalCount} products matching '{q}'",
                    Data = products,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return StatusCode(500, new ApiResponse<List<ProductModel>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
}
