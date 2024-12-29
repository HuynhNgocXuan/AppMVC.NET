using System.ComponentModel.DataAnnotations;
using webMVC.Models.Blog;

namespace webMVC.Areas.Blog.Models
{
    public class CreatePostModel : Post
    {
        [Display(Name = "Chuyên mục")]
        public int[]? CategoryIDs { get; set; }
    }
}