using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webMVC.Models;

namespace webMVC.Areas.DataBase.Controllers
{

    [Area("DataBase")]
    [Route("database/{action=Index}")]
    public class DbManageController : Controller
    {
        private readonly AppDbContext _dbContext;

        public DbManageController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
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

        [TempData]
        public string statusMessage { get; set; }

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
    }
}
