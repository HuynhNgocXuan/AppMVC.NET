using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webMVC.Models;

namespace webMVC.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {

        var products = _context.Products!
                               .Include(p => p.Author)
                               .Include(p => p.Photos)
                               .Include(p => p.CategoryAndProducts)!
                               .ThenInclude(p => p.Category)
                               .AsQueryable();

        products = products.OrderByDescending(p => p.DateUpdated).Take(14);

        ViewBag.products = products;


        var posts = _context.Posts!
                                .Include(p => p.Author)
                                .Include(p => p.PostCategories!)
                                .ThenInclude(p => p.Category)
                                .AsQueryable();

        posts = posts.OrderByDescending(p => p.DateUpdated).Take(10);

        ViewBag.posts = posts;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
