using webMVC.Data;
using webMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public string statusMessage { get; set; }


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

            statusMessage = success ? "Xóa Database thành công" : "Không xóa được Db";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Migrate()
        {
            await _dbContext.Database.MigrateAsync();

            statusMessage = "Cập nhật Database thành công";

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
                            statusMessage = $"Không thể tạo vai trò {roleName}. Lỗi: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}";
                            return RedirectToAction("Index", new { message = statusMessage });
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
                        statusMessage = $"Không thể thêm người dùng vào vai trò. Lỗi: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}";
                        return RedirectToAction("Index", new { message = statusMessage });
                    }

                    statusMessage = "Vừa seed Database thành công";
                }
                else
                {
                    // Xử lý lỗi khi tạo người dùng
                    statusMessage = $"Không thể tạo người dùng admin. Lỗi: {string.Join(", ", createResult.Errors.Select(e => e.Description))}";
                    return RedirectToAction("Index", new { message = statusMessage });
                }
            }
            else
            {
                statusMessage = "Seed Database đã có";
            }

            return RedirectToAction("Index", new { message = statusMessage });
        }

    }
}
