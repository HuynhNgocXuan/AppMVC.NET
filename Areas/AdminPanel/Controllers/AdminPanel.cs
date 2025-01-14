using webMVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace webMVC.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    [Authorize(Roles = RoleName.Administrator)]
    public class AdminPanel  : Controller
    {
        [Route("/admin-panel/")]
        public IActionResult Index()  => View();
    }
}