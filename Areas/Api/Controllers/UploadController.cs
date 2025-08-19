using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using webMVC.Models;
using webMVC.Models.Product;
using webMVC.Data;

namespace webMVC.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UploadController> _logger;
        private readonly IWebHostEnvironment _environment;

        public UploadController(AppDbContext context, ILogger<UploadController> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        // POST: api/upload/product-photos
        [HttpPost("product-photos")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<ApiResponse<List<object>>>> UploadProductPhotos(int productId, [FromForm] List<IFormFile> files)
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return BadRequest(new ApiResponse<List<object>>
                    {
                        Success = false,
                        Message = "No files provided"
                    });
                }

                var product = await _context.Products!.FindAsync(productId);
                if (product == null)
                {
                    return NotFound(new ApiResponse<List<object>>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                var uploadedFiles = new List<object>();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Validate file extension
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            continue; // Skip invalid files
                        }

                        // Generate unique filename
                        var fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + extension;
                        var filePath = Path.Combine(_environment.ContentRootPath, "Uploads", "Products", fileName);

                        // Ensure directory exists
                        var directory = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory!);
                        }

                        // Save file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        // Save to database
                        var productPhoto = new ProductPhoto
                        {
                            ProductID = productId,
                            FileName = fileName
                        };

                        _context.Add(productPhoto);
                        uploadedFiles.Add(new
                        {
                            Id = productPhoto.Id,
                            FileName = fileName,
                            Url = $"/contents/Products/{fileName}",
                            Size = file.Length
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = $"Successfully uploaded {uploadedFiles.Count} files",
                    Data = uploadedFiles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product photos");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // DELETE: api/upload/product-photos/{photoId}
        [HttpDelete("product-photos/{photoId}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProductPhoto(int photoId)
        {
            try
            {
                var photo = await _context.ProductPhotos!.FindAsync(photoId);
                if (photo == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Photo not found"
                    });
                }

                // Delete file from disk
                var filePath = Path.Combine(_environment.ContentRootPath, "Uploads", "Products", photo.FileName!);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Delete from database
                _context.ProductPhotos!.Remove(photo);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Photo deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product photo {PhotoId}", photoId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // GET: api/upload/product-photos/{productId}
        [HttpGet("product-photos/{productId}")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetProductPhotos(int productId)
        {
            try
            {
                var photos = await _context.ProductPhotos!
                    .Where(p => p.ProductID == productId)
                    .Select(p => new
                    {
                        Id = p.Id,
                        FileName = p.FileName,
                        Url = $"/contents/Products/{p.FileName}"
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Product photos retrieved successfully",
                    Data = photos.Cast<object>().ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product photos for product {ProductId}", productId);
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // POST: api/upload/general
        [HttpPost("general")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<object>>>> UploadGeneralFiles([FromForm] List<IFormFile> files, [FromQuery] string? folder = "general")
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return BadRequest(new ApiResponse<List<object>>
                    {
                        Success = false,
                        Message = "No files provided"
                    });
                }

                var uploadedFiles = new List<object>();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".txt" };

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Validate file extension
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            continue; // Skip invalid files
                        }

                        // Generate unique filename
                        var fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + extension;
                        var filePath = Path.Combine(_environment.ContentRootPath, "Uploads", folder, fileName);

                        // Ensure directory exists
                        var directory = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory!);
                        }

                        // Save file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        uploadedFiles.Add(new
                        {
                            FileName = fileName,
                            OriginalName = file.FileName,
                            Url = $"/contents/{folder}/{fileName}",
                            Size = file.Length,
                            ContentType = file.ContentType
                        });
                    }
                }

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = $"Successfully uploaded {uploadedFiles.Count} files",
                    Data = uploadedFiles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading general files");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
}
