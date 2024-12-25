using System.Security.Claims;
using webMVC.Areas.Identity.Models.RoleViewModels;
using webMVC.Data;
using webMVC.ExtendMethods;
using webMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace webMVC.Areas.Identity.Controllers
{

    [Authorize(Roles = RoleName.Administrator)]
    [Area("Identity")]
    [Route("/Role/[action]")]
    public class RoleController : Controller
    {

        private readonly ILogger<RoleController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        private readonly UserManager<AppUser> _userManager;

        public RoleController(ILogger<RoleController> logger, RoleManager<IdentityRole> roleManager, AppDbContext context, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
            _userManager = userManager;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        //
        // GET: /Role/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {

            var r = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var roles = new List<RoleModel>();
            foreach (var _r in r)
            {
                var claims = await _roleManager.GetClaimsAsync(_r);
                var claimsString = claims.Select(c => c.Type + " = " + c.Value);

                var rm = new RoleModel()
                {
                    Name = _r.Name,
                    Id = _r.Id,
                    Claims = claimsString.ToArray()
                };
                roles.Add(rm);
            }

            return View(roles);
        }

        // GET: /Role/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Role/Create
        [HttpPost, ActionName(nameof(Create))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(CreateRoleModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var newRole = new IdentityRole(model.Name!);
            var result = await _roleManager.CreateAsync(newRole);
            if (result.Succeeded)
            {
                StatusMessage = $"Bạn vừa tạo role mới: {model.Name}";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError(result);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
                return NotFound("Không tìm thấy role");

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                return NotFound("Không tìm thấy role");

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = $"Bạn vừa xóa vai trò: {role.Name}";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View("Index", await _roleManager.Roles.ToListAsync());
        }


        // GET: /Role/Edit/roleId
        [HttpGet("{roleId}")]
        public async Task<IActionResult> EditAsync(string roleId, [Bind("Name")] EditRoleModel model)
        {
            if (roleId == null) return NotFound("Không tìm thấy role");
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            model.Name = role.Name;
            model.Claims = await _context.RoleClaims.Where(rc => rc.RoleId == role.Id).ToListAsync();
            model.role = role;
            ModelState.Clear();
            return View(model);

        }

        // POST: /Role/Edit/1
        [HttpPost("{roleId}"), ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConfirmAsync(string roleId, [Bind("Name")] EditRoleModel model)
        {
            if (roleId == null) return NotFound("Không tìm thấy role");
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            model.Claims = await _context.RoleClaims.Where(rc => rc.RoleId == role.Id).ToListAsync();
            model.role = role;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                StatusMessage = $"Bạn vừa đổi tên: {model.Name}";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError(result);
            }

            return View(model);
        }

        // GET: /Role/AddRoleClaim/roleId
        [HttpGet("{roleId}")]
        public async Task<IActionResult> AddRoleClaimAsync(string roleId)
        {
            if (roleId == null) return NotFound("Không tìm thấy role");
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) 
            {
                return NotFound("Không tìm thấy role");
            }

            var model = new EditClaimModel()
            {
                role = role
            };
            return View(model);
        }

        // POST: /Role/AddRoleClaim/roleId
        [HttpPost("{roleId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoleClaimAsync(string roleId, [Bind("ClaimType", "ClaimValue")] EditClaimModel model)
        {
            if (roleId == null) return NotFound("Không tìm thấy role");
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            model.role = role;
            if (!ModelState.IsValid) return View(model);


            if ((await _roleManager.GetClaimsAsync(role)).Any(c => c.Type == model.ClaimType && c.Value == model.ClaimValue))
            {
                ModelState.AddModelError(string.Empty, "Claim này đã có trong role");
                return View(model);
            }

            var newClaim = new Claim(model.ClaimType!, model.ClaimValue!);
            var result = await _roleManager.AddClaimAsync(role, newClaim);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(result);
                return View(model);
            }

            StatusMessage = "Vừa thêm đặc tính (claim) mới";

            return RedirectToAction("Edit", new { roleId = role.Id });

        }

        // GET: /Role/EditRoleClaim/claimed
        [HttpGet("{claimed:int}")]
        public async Task<IActionResult> EditRoleClaim(int claimed)
        {
            var claim = _context.RoleClaims.Where(c => c.Id == claimed).FirstOrDefault();
            if (claim == null) return NotFound("Không tìm thấy role");

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null) return NotFound("Không tìm thấy role");
            ViewBag.claimed = claimed;

            var Input = new EditClaimModel()
            {
                ClaimType = claim.ClaimType,
                ClaimValue = claim.ClaimValue,
                role = role
            };


            return View(Input);
        }

        // GET: /Role/EditRoleClaim/claimed
        [HttpPost("{claimed:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoleClaim(int claimed, [Bind("ClaimType", "ClaimValue")] EditClaimModel Input)
        {
            var claim = _context.RoleClaims.Where(c => c.Id == claimed).FirstOrDefault();
            if (claim == null) return NotFound("Không tìm thấy role");

            ViewBag.claimed = claimed;

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null) return NotFound("Không tìm thấy role");
            Input.role = role;
            if (!ModelState.IsValid)
            {
                return View(Input);
            }
            if (_context.RoleClaims.Any(c => c.RoleId == role.Id && c.ClaimType == Input.ClaimType && c.ClaimValue == Input.ClaimValue && c.Id != claim.Id))
            {
                ModelState.AddModelError(string.Empty, "Claim này đã có trong role");
                return View(Input);
            }


            claim.ClaimType = Input.ClaimType;
            claim.ClaimValue = Input.ClaimValue;

            await _context.SaveChangesAsync();

            StatusMessage = "Vừa cập nhật claim";

            return RedirectToAction("Edit", new { roleId = role.Id });
        }
        // POST: /Role/EditRoleClaim/claimed
        [HttpPost("{claimed:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClaim(int claimed, [Bind("ClaimType", "ClaimValue")] EditClaimModel Input)
        {
            var claim = _context.RoleClaims.Where(c => c.Id == claimed).FirstOrDefault();
            if (claim == null) return NotFound("Không tìm thấy role");

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null) return NotFound("Không tìm thấy role");
            Input.role = role;
            if (!ModelState.IsValid)
            {
                return View(Input);
            }
            if (_context.RoleClaims.Any(c => c.RoleId == role.Id && c.ClaimType == Input.ClaimType && c.ClaimValue == Input.ClaimValue && c.Id != claim.Id))
            {
                ModelState.AddModelError(string.Empty, "Claim này đã có trong role");
                return View(Input);
            }


            await _roleManager.RemoveClaimAsync(role, new Claim(claim.ClaimType!, claim.ClaimValue!));

            StatusMessage = "Vừa xóa claim";


            return RedirectToAction("Edit", new { roleId = role.Id });
        }


    }
} 
