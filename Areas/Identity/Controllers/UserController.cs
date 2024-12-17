// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using webMVC.Areas.Identity.Models.AccountViewModels;
using webMVC.Areas.Identity.Models.ManageViewModels;
using webMVC.Areas.Identity.Models.RoleViewModels;
using webMVC.Areas.Identity.Models.UserViewModels;
using webMVC.Data;
using webMVC.ExtendMethods;
using webMVC.Models;
using webMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace webMVC.Areas.Identity.Controllers
{

    [Authorize(Roles = RoleName.Administrator)]
    [Area("Identity")]
    [Route("/ManageUser/[action]")]
    public class UserController : Controller
    {
        private readonly ILogger<RoleController> _logger;

        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly AppDbContext _context;

        private readonly UserManager<AppUser> _userManager;

        [TempData]
        public string? StatusMessage { get; set; }

        public UserController(ILogger<RoleController> logger, RoleManager<IdentityRole> roleManager, AppDbContext context, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
            _userManager = userManager;
        }

        //
        // GET: /ManageUser/Index
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage)
        {
            var model = new UserListModel();
            model.currentPage = currentPage;

            var usersQuery = _userManager.Users.OrderBy(u => u.UserName).Select(u => new UserAndRole()
            {
                Id = u.Id,
                UserName = u.UserName,
            });

            var paginationResult = await PaginationHelper.PaginateAsync(usersQuery, model.currentPage, model.ITEMS_PER_PAGE);

            model.users = paginationResult.items;
            model.totalUsers = paginationResult.totalItems;
            model.countPages = paginationResult.countPages;
            model.currentPage = paginationResult.currentPage;

            foreach (var user in model.users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                user.RoleNames = string.Join(", ", roles);
            }

            return View(model);
        }

        // GET: /ManageUser/AddRole/id
        [HttpGet("{id}")]
        public async Task<IActionResult> AddRoleAsync(string id, [FromQuery(Name = "p")] int? currentPage = null)
        {
            var model = new AddUserRoleModel();
            if (string.IsNullOrEmpty(id))
            {
                return NotFound($"Không có user");
            }

            model.user = await _userManager.FindByIdAsync(id);

            if (model.user == null)
            {
                return NotFound($"Không thấy user, id = {id}.");
            }

            model.RoleNames = (await _userManager.GetRolesAsync(model.user)).ToArray<string>();

            List<string?> roleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.allRoles = new SelectList(roleNames);
            ViewBag.currentPage = currentPage;

            await GetClaims(model);

            return View(model);
        }

        // GET: /ManageUser/AddRole/id
        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoleAsync(string id, [Bind("RoleNames")] AddUserRoleModel model, [FromQuery(Name = "p")] int? currentPage)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound($"Không có user");
            }

            model.user = await _userManager.FindByIdAsync(id);

            if (model.user == null)
            {
                return NotFound($"Không thấy user, id = {id}.");
            }

            await GetClaims(model);

            var OldRoleNames = (await _userManager.GetRolesAsync(model.user)).ToArray();
            var deleteRoles = OldRoleNames.Where(r => !model.RoleNames.Contains(r));
            var addRoles = model.RoleNames.Where(r => !OldRoleNames.Contains(r));

            List<string?> roleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.allRoles = new SelectList(roleNames);

            var resultDelete = await _userManager.RemoveFromRolesAsync(model.user, deleteRoles);
            if (!resultDelete.Succeeded)
            {
                ModelState.AddModelError(resultDelete);
                return View(model);
            }

            var resultAdd = await _userManager.AddToRolesAsync(model.user, addRoles);
            if (!resultAdd.Succeeded)
            {
                ModelState.AddModelError(resultAdd);
                return View(model);
            }


            StatusMessage = $"Vừa cập nhật role cho user: {model.user.UserName}";

            return RedirectToAction("Index", new { p = currentPage });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> SetPasswordAsync(string id, [FromQuery(Name = "p")] int? currentPage)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("Không có user");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound($"Không thấy user, id = {id}.");
            }

            ViewBag.user = user; 
            ViewBag.currentPage = currentPage; 

            return View();
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPasswordAsync(string id, SetUserPasswordModel model, [FromQuery(Name = "p")] int? currentPage)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("Không có user");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound($"Không thấy user, id = {id}.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _userManager.RemovePasswordAsync(user);

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword!);
            if (!addPasswordResult.Succeeded)
            {
                foreach (var error in addPasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            StatusMessage = $"Vừa cập nhật mật khẩu cho user: {user.UserName}";
            return RedirectToAction("Index", new { p = currentPage }); 
        }

        [HttpGet("{userid}")]
        public async Task<ActionResult> AddClaimAsync(string userid, [FromQuery(Name = "p")] int? currentPage)
        {
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null) return NotFound("Không tìm thấy user");
            ViewBag.user = user;
            ViewBag.currentPage = currentPage;
            return View();
        }

        [HttpPost("{userid}")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddClaimAsync(string userid, AddUserClaimModel model, [FromQuery(Name = "p")] int? currentPage)
        {

            var user = await _userManager.FindByIdAsync(userid);
            if (user == null) return NotFound("Không tìm thấy user");

            ViewBag.user = user;
            if (!ModelState.IsValid) return View(model);

            var claims = _context.UserClaims.Where(c => c.UserId == user.Id);

            if (claims.Any(c => c.ClaimType == model.ClaimType && c.ClaimValue == model.ClaimValue))
            {
                ModelState.AddModelError(string.Empty, "Đặc tính này đã có");
                return View(model);
            }

            await _userManager.AddClaimAsync(user, new Claim(model.ClaimType!, model.ClaimValue!));
            StatusMessage = "Đã thêm đặc tính cho user";

            return RedirectToAction("AddRole", new { id = user.Id, p = currentPage });
        }

        [HttpGet("{claimed}")]
        public async Task<IActionResult> EditClaim(int claimed)
        {
            var userClaim = _context.UserClaims.FirstOrDefault(c => c.Id == claimed);
            if (userClaim == null)
                return NotFound("Không tìm thấy claim");

            var user = await _userManager.FindByIdAsync(userClaim.UserId);
            if (user == null)
                return NotFound("Không tìm thấy user");

            var model = new AddUserClaimModel
            {
                ClaimType = userClaim.ClaimType,
                ClaimValue = userClaim.ClaimValue
            };

            ViewBag.user = user;  
            ViewBag.userClaim = userClaim;  
            return View("AddClaim", model);
        }


        [HttpPost("{claimed}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClaim(int claimed, AddUserClaimModel model, [FromQuery(Name = "p")] int? currentPage)
        {
            var userClaim = _context.UserClaims.FirstOrDefault(c => c.Id == claimed);
            if (userClaim == null)
                return NotFound("Không tìm thấy claim");

            var user = await _userManager.FindByIdAsync(userClaim.UserId);
            if (user == null)
                return NotFound("Không tìm thấy user");

            if (!ModelState.IsValid)
                return View("AddClaim", model);

            var duplicateClaim = _context.UserClaims.FirstOrDefault(c =>
                c.UserId == user.Id &&
                c.ClaimType == model.ClaimType &&
                c.ClaimValue == model.ClaimValue &&
                c.Id != userClaim.Id);

            if (duplicateClaim != null)
            {
                ModelState.AddModelError(string.Empty, $"Claim với Type '{model.ClaimType}' và Value '{model.ClaimValue}' đã tồn tại.");
                return View("AddClaim", model);
            }

            userClaim.ClaimType = model.ClaimType;
            userClaim.ClaimValue = model.ClaimValue;

            await _context.SaveChangesAsync();
            StatusMessage = "Bạn vừa cập nhật claim";

            return RedirectToAction("AddRole", new { id = user.Id, p = currentPage });
        }



        [HttpPost("{claimed}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClaimAsync(int claimed, [FromQuery(Name = "p")] int? currentPage)
        {
            var userClaim = _context.UserClaims.Where(c => c.Id == claimed).FirstOrDefault();
            var user = await _userManager.FindByIdAsync(userClaim!.UserId);

            if (user == null) return NotFound("Không tìm thấy user");

            await _userManager.RemoveClaimAsync(user, new Claim(userClaim.ClaimType!, userClaim.ClaimValue!));

            StatusMessage = "Bạn đã xóa claim";

            return RedirectToAction("AddRole", new { id = user.Id, p = currentPage });
        }

        private async Task GetClaims(AddUserRoleModel model)
        {
            var listRoles = from r in _context.Roles
                            join ur in _context.UserRoles on r.Id equals ur.RoleId
                            where ur.UserId == model.user!.Id
                            select r;

            var _claimsInRole = from c in _context.RoleClaims
                                join r in listRoles on c.RoleId equals r.Id
                                select c;
            model.claimsInRole = await _claimsInRole.ToListAsync();


            model.claimsInUserClaim = await (from c in _context.UserClaims
                                             where c.UserId == model.user!.Id
                                             select c).ToListAsync();
        }
    }
}
