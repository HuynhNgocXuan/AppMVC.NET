using webMVC.Data;
using webMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bogus;
using webMVC.Models.Blog;
using webMVC.Models.Product;

namespace webMVC.Areas.DataBase.Controllers
{

    [Area("DataBase")]
    [Route("database/{action=Index}")]
    public class DbManageController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger _logger;

        [TempData]
        public string? StatusMessage { get; set; }


        public DbManageController(ILogger<DbManageController> logger, AppDbContext dbContext, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: DbManage 
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DeleteDb()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> DeleteDbAsync()
        {
            var success = await _dbContext.Database.EnsureDeletedAsync();

            StatusMessage = success ? "Xóa Database thành công" : "Không xóa được Db";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Migrate()
        {
            await _dbContext.Database.MigrateAsync();

            StatusMessage = "Cập nhật Database thành công";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SeedDataAsync()
        {
            // Lấy tất cả các tên vai trò từ enum hoặc các trường đã định nghĩa
            var roleNames = typeof(RoleName).GetFields().ToList();

            // Kiểm tra và tạo các vai trò nếu chưa tồn tại
            foreach (var r in roleNames)
            {
                var roleName = r.GetRawConstantValue() as string;
                if (!string.IsNullOrEmpty(roleName))
                {
                    var roleFound = await _roleManager.FindByNameAsync(roleName);
                    if (roleFound == null)
                    {
                        var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));
                        if (!roleResult.Succeeded)
                        {
                            // Xử lý lỗi nếu không thể tạo vai trò
                            StatusMessage = $"Không thể tạo vai trò {roleName}. Lỗi: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}";
                            return RedirectToAction("Index", new { message = StatusMessage });
                        }
                    }
                }
            }

            // Kiểm tra nếu người dùng admin đã tồn tại
            var userAdmin = await _userManager.FindByEmailAsync("admin@example.com");
            if (userAdmin == null)
            {
                userAdmin = new AppUser()
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    EmailConfirmed = true,
                    HomeAddress = "26/48 phường 1, Quận 1, Hồ Chí Minh",
                };

                var createResult = await _userManager.CreateAsync(userAdmin, "123Wolf#");
                if (createResult.Succeeded)
                {
                    var addToRoleResult = await _userManager.AddToRoleAsync(userAdmin, RoleName.Administrator);
                    if (!addToRoleResult.Succeeded)
                    {
                        // Xử lý lỗi khi thêm vào vai trò
                        StatusMessage = $"Không thể thêm người dùng vào vai trò. Lỗi: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}";
                        return RedirectToAction("Index", new { message = StatusMessage });
                    }

                    StatusMessage = "Vừa seed Database thành công";
                }
                else
                {
                    // Xử lý lỗi khi tạo người dùng
                    StatusMessage = $"Không thể tạo người dùng admin. Lỗi: {string.Join(", ", createResult.Errors.Select(e => e.Description))}";
                    return RedirectToAction("Index", new { message = StatusMessage });
                }
            }
            else
            {
                StatusMessage = "Seed Database đã có";
                _logger.LogInformation(StatusMessage);
            }

            SeedPostCategory();
            SeedProductCategory();

            return RedirectToAction("Index", new { message = StatusMessage });
        }

        private void SeedProductCategory()
        {

            _dbContext.CategoryProducts?.RemoveRange(_dbContext.CategoryProducts.Where(c => c.Description!.Contains("[fakeData]")));
            _dbContext.Products?.RemoveRange(_dbContext.Products.Where(p => p.Content!.Contains("[fakeData]")));

            _dbContext.SaveChanges();

            var fakerCategory = new Faker<CategoryProduct>();
            int cm = 1;
            fakerCategory.RuleFor(c => c.Title, fk => $"Nhom SP{cm++} " + fk.Lorem.Sentence(1, 2).Trim('.'));
            fakerCategory.RuleFor(c => c.Description, fk => fk.Lorem.Sentences(5) + "[fakeData]");
            fakerCategory.RuleFor(c => c.Slug, fk => fk.Lorem.Slug());



            var cate1 = fakerCategory.Generate();
            var cate11 = fakerCategory.Generate();
            var cate12 = fakerCategory.Generate();
            var cate2 = fakerCategory.Generate();
            var cate21 = fakerCategory.Generate();
            var cate211 = fakerCategory.Generate();


            cate11.ParentCategory = cate1;
            cate12.ParentCategory = cate1;
            cate21.ParentCategory = cate2;
            cate211.ParentCategory = cate21;

            var categories = new CategoryProduct[] { cate1, cate2, cate12, cate11, cate21, cate211 };
            _dbContext.CategoryProducts?.AddRange(categories);



            
            var rCateIndex = new Random();
            int bv = 1;

            var user = _userManager.GetUserAsync(this.User).Result;
            var fakerProduct = new Faker<ProductModel>();
            fakerProduct.RuleFor(p => p.AuthorId, f => user?.Id);
            fakerProduct.RuleFor(p => p.Content, f => f.Commerce.ProductDescription() + "[fakeData]");
            fakerProduct.RuleFor(p => p.DateCreated, f => f.Date.Between(new DateTime(2021, 1, 1), new DateTime(2021, 7, 1)));
            fakerProduct.RuleFor(p => p.Description, f => f.Lorem.Sentences(3));
            fakerProduct.RuleFor(p => p.Published, f => true);
            fakerProduct.RuleFor(p => p.Slug, f => f.Lorem.Slug());
            fakerProduct.RuleFor(p => p.Title, f => $"SP {bv++} " + f.Commerce.ProductName());
            fakerProduct.RuleFor(p => p.Price, f => int.Parse(f.Commerce.Price(500, 1000, 0)));

            List<ProductModel> products = new List<ProductModel>();
            List<CategoryAndProduct> product_categories = new List<CategoryAndProduct>();


            for (int i = 0; i < 40; i++)
            {
                var product = fakerProduct.Generate();
                product.DateUpdated = product.DateCreated;
                products.Add(product);
                product_categories.Add(new CategoryAndProduct()
                {
                    Product = product,
                    Category = categories[rCateIndex.Next(5)]
                });
            }
            _dbContext.AddRange(products);
            _dbContext.AddRange(product_categories);

            _dbContext.SaveChanges();
        }





        private void SeedPostCategory()
        {
            _dbContext.Categories!.RemoveRange(_dbContext.Categories.Where(c => c.Description!.Contains("[fakeData]")));
            _dbContext.Posts!.RemoveRange(_dbContext.Posts.Where(p => p.Content!.Contains("[fakeData]")));


            var fakerCategory = new Faker<Category>();
            int cm = 1;
            fakerCategory.RuleFor(c => c.Title, fk => $"Chuyên mục {cm++} " + fk.Lorem.Sentence(1, 4).Trim('.'));
            fakerCategory.RuleFor(c => c.Description, fk => fk.Lorem.Sentences(5) + "[fakeData]");
            fakerCategory.RuleFor(c => c.Slug, fk => fk.Lorem.Slug());



            var cate1 = fakerCategory.Generate();
            var cate11 = fakerCategory.Generate();
            var cate12 = fakerCategory.Generate();
            var cate2 = fakerCategory.Generate();
            var cate21 = fakerCategory.Generate();
            var cate211 = fakerCategory.Generate();


            cate11.ParentCategory = cate1;
            cate12.ParentCategory = cate1;
            cate21.ParentCategory = cate2;
            cate211.ParentCategory = cate21;

            var categories = new Category[] { cate1, cate2, cate12, cate11, cate21, cate211 };
            _dbContext.Categories.AddRange(categories);



            var rCateIndex = new Random();
            int bv = 1;

            var user = _userManager.GetUserAsync(this.User).Result;
            var fakerPost = new Faker<Post>();
            fakerPost.RuleFor(p => p.AuthorId, f => user!.Id);
            fakerPost.RuleFor(p => p.Content, f => f.Lorem.Paragraphs(7) + "[fakeData]");
            fakerPost.RuleFor(p => p.DateCreated, f => f.Date.Between(new DateTime(2021, 1, 1), new DateTime(2021, 7, 1)));
            fakerPost.RuleFor(p => p.Description, f => f.Lorem.Sentences(3));
            fakerPost.RuleFor(p => p.Published, f => true);
            fakerPost.RuleFor(p => p.Slug, f => f.Lorem.Slug());
            fakerPost.RuleFor(p => p.Title, f => $"Bài {bv++} " + f.Lorem.Sentence(3, 4).Trim('.'));

            List<Post> posts = new List<Post>();
            List<PostCategory> post_categories = new List<PostCategory>();


            for (int i = 0; i < 40; i++)
            {
                var post = fakerPost.Generate();
                post.DateUpdated = post.DateCreated;
                posts.Add(post);
                post_categories.Add(new PostCategory()
                {
                    Post = post,
                    Category = categories[rCateIndex.Next(5)]
                });
            }

            _dbContext.AddRange(posts);
            _dbContext.AddRange(post_categories);

            _dbContext.SaveChanges();
        }
    }
}
