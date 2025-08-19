using System.ComponentModel.DataAnnotations;

namespace webMVC.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int? TotalCount { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int? TotalPages { get; set; }
    }

    public class ApiPaginationRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "asc"; // asc or desc
        public string? SearchTerm { get; set; }
    }

    public class ApiErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public object? Errors { get; set; }
        public string? TraceId { get; set; }
    }
}
