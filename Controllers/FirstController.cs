using Microsoft.AspNetCore.Mvc;
using webMVC.Services;

namespace webMVC.Controllers;

public class FirstController : Controller 
{
    private readonly ILogger _logger;
    private readonly ProductService _productService;


    public FirstController(ILogger<FirstController> logger, ProductService productService) 
    {
        _logger = logger;
        _productService = productService;
    }

    public string Index() 
    {
        _logger.LogInformation("Warning Xuan");
        
        return "First   Controller";
    }

    public IActionResult HelloView(string? name) 
    {
        if (string.IsNullOrEmpty(name)) name = "Xuan";
         
        return View("MyView/Hello.cshtml", name);
    }

     public IActionResult HiView(string name) 
    {
        if (string.IsNullOrEmpty(name)) name = "Xuan";
         
        return View();
        // return View((object)model);
    }

    public IActionResult ProductView(int? id) 
    {
        var product = _productService.Where(p => p.Id == id).FirstOrDefault();

        if (product == null) 
        {
            _logger.LogInformation("Home xuan");
            TempData["statusMessage"] = "Not Found";
            var url = Url.Action("index", "Home");
            if (url == null) return View();
            return Redirect(url.ToString());    
        }
        return View(product);
    }
}