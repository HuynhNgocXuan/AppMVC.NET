// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using webMVC.Areas.Identity.Models.ManageViewModels;
using webMVC.ExtendMethods;
using webMVC.Models;
using webMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace webMVC.Areas.Identity.Controllers
{

    [Authorize]
    [Area("Identity")]
    [Route("/InfoManage/[action]")]
    public class ManageController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;

        private readonly ISmsSender _smsSender;

        private readonly IEncryptionService _encryptionService;

        private readonly IQRCodeService _qrCodeService;

        private readonly ILogger<ManageController> _logger;


        public ManageController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IEncryptionService encryptionService,
        IQRCodeService qrCodeService,
        ILogger<ManageController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _smsSender = smsSender;
            _encryptionService = encryptionService;
            _qrCodeService = qrCodeService;;
        }

        //
        // GET: /Manage/Index
        [HttpGet]
        public async Task<IActionResult> Index(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.ChangePasswordSuccess ? "Đã thay đổi mật khẩu."
                : message == ManageMessageId.SetPasswordSuccess ? "Đã đặt lại mật khẩu."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "Có lỗi."
                : message == ManageMessageId.AddPhoneSuccess ? "Đã thêm số điện thoại."
                : message == ManageMessageId.RemovePhoneSuccess ? "Đã bỏ số điện thoại."
                : message == ManageMessageId.EnableAuthenticator ? "Bạn cần bật xác thực hai yếu tố trước khi phát sinh khóa mới."
                : "";

            var user = await GetCurrentUserAsync();
            if (user == null) return View();

            var encryptedPhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            string? decryptedPhoneNumber = null;

            if (!string.IsNullOrWhiteSpace(encryptedPhoneNumber))
            {
                try
                {
                    decryptedPhoneNumber = await _encryptionService.Decrypt(encryptedPhoneNumber);
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Không thể giải mã số điện thoại.");
                }
            } 

            var model = new IndexViewModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user),
                PhoneNumber = decryptedPhoneNumber,
                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                Logins = await _userManager.GetLoginsAsync(user),
                BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user),
                AuthenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user),
                profile = new EditExtraProfileModel()
                {
                    BirthDate = user.BirthDate,
                    HomeAddress = user.HomeAddress,
                    UserName = user.UserName,
                    UserEmail = user.Email,
                    PhoneNumber = decryptedPhoneNumber,
                }
            };
            return View(model);
        }
        public enum ManageMessageId
        {
            AddPhoneSuccess,
            AddLoginSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error,
            EnableAuthenticator
        }
        private async Task<AppUser?> GetCurrentUserAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                ModelState.AddModelError("Không tìm thấy người dùng");
            }
            return user;
        }

        //
        // GET: /Manage/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var passwordCheck = await _userManager.CheckPasswordAsync(user, model.NewPassword);
                if (passwordCheck)
                {
                    ModelState.AddModelError(string.Empty, "Mật khẩu mới không được giống với mật khẩu hiện tại.");
                    return View(model);
                }

                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User changed their password successfully.");
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
                }
                ModelState.AddModelError(result);
                return View(model);
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }
        //
        // GET: /Manage/SetPassword
        [HttpGet]
        public IActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            } 

            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.AddPasswordAsync(user, model.NewPassword!);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.SetPasswordSuccess });
                }
                ModelState.AddModelError(result);
                return View(model);
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }

        //GET: /Manage/ManageLogins
        [HttpGet]
        public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.RemoveLoginSuccess ? "Đã loại bỏ liên kết tài khoản."
                : message == ManageMessageId.AddLoginSuccess ? "Đã thêm liên kết tài khoản"
                : message == ManageMessageId.Error ? "Có lỗi."
                : "";

            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            var userLogins = await _userManager.GetLoginsAsync(user);
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var otherLogins = schemes.Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();
            ViewData["ShowRemoveButton"] = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }


        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LinkLogin(string provider)
        {
            var redirectUrl = Url.Action("LinkLoginCallback", "Manage");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return Challenge(properties, provider);
        }

        //
        // GET: /Manage/LinkLoginCallback
        [HttpGet]
        public async Task<ActionResult> LinkLoginCallback()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var info = await _signInManager.GetExternalLoginInfoAsync(await _userManager.GetUserIdAsync(user));
            if (info == null)
            {
                return RedirectToAction(nameof(ManageLogins), new { Message = ManageMessageId.Error });
            }
            var result = await _userManager.AddLoginAsync(user, info);
            var message = result.Succeeded ? ManageMessageId.AddLoginSuccess : ManageMessageId.Error;
            return RedirectToAction(nameof(ManageLogins), new { Message = message });
        }


        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel account)
        {
            ManageMessageId? message = ManageMessageId.Error;
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    message = ManageMessageId.RemoveLoginSuccess;
                }
            }
            return RedirectToAction(nameof(ManageLogins), new { Message = message });
        }
        //
        // GET: /Manage/AddPhoneNumber
        public IActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                ModelState.AddModelError("User", "Không tìm thấy người dùng.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber)) return View(model);


            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError("Code", "Không thể tạo mã xác thực.");
                return View(model);
            }

            if (_smsSender == null)
            {
                ModelState.AddModelError("SmsSender", "Dịch vụ SMS không khả dụng.");
                return View(model);
            }

            await _smsSender.SendSmsAsync(model.PhoneNumber, "Mã xác thực là: " + code);

            return RedirectToAction(nameof(VerifyPhoneNumber), new { PhoneNumber = model.PhoneNumber });
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        [HttpGet]
        public async Task<IActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return View();
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await GetCurrentUserAsync();
            if (user == null) return View(model);

            var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);

            if (result.Succeeded)
            {

                var encryptedPhoneNumber = await _encryptionService.Encrypt(model.PhoneNumber);
                user.PhoneNumber = encryptedPhoneNumber;
                await _userManager.UpdateAsync(user);

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.AddPhoneSuccess });
            }

            var errorMessage = result.Errors.Any(e => e.Code == "InvalidToken")
            ? "Mã xác thực không đúng hoặc đã hết hạn."
            : "Lỗi thêm số điện thoại.";

            ModelState.AddModelError(string.Empty, errorMessage);
            return View(model);
        }
        //
        // GET: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.SetPhoneNumberAsync(user, null);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.RemovePhoneSuccess });
                }
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }


        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            return RedirectToAction(nameof(Index), "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogWarning("Attempt to disable 2FA for a non-existent user.");
                return View("Error");
            }

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                _logger.LogWarning("Attempt to disable 2FA for user where 2FA is already disabled.");
                TempData["Notify"] = "Xác thực hai yếu tố đã bị tắt trước đó.";
                return RedirectToAction(nameof(Index), "Manage");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {UserId} disabled two-factor authentication.", user.Id);

            TempData["Notify"] = "Xác thực hai yếu tố đã được tắt.";
            return RedirectToAction(nameof(Index), "Manage");
        }

        //
        // POST: /Manage/ResetAuthenticatorKey
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAuthenticatorKey()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogWarning("Attempt to reset authenticator key for a non-existent user.");
                return View("Error");
            }

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                _logger.LogWarning("Attempt to reset authenticator key for user with 2FA disabled.");
                return RedirectToAction(nameof(Index), "Manage", new { Message = ManageMessageId.EnableAuthenticator });
            }

            await _userManager.ResetAuthenticatorKeyAsync(user);
            var newKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(newKey) || user.Email == null)
            {
                return View("Error");
            }

            string issuer = "YourAppName"; 
            string accountName = user.Email; 
            string qrCodeUri = $"otpauth://totp/{issuer}:{accountName}?secret={newKey}&issuer={issuer}";


            string qrCodeBase64 = _qrCodeService.GenerateQrCode(qrCodeUri);

            var model = new ResetAuthenticatorKeyViewModel
            {
                AuthenticatorKey = newKey,
                QrCodeBase64 = qrCodeBase64
            };

            return View(nameof(ResetAuthenticatorKey), model);
        }

        //
        // POST: /Manage/GenerateRecoveryCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCode()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogWarning("Attempt to generate recovery codes for a non-existent user.");
                return View("Error");
            }

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                _logger.LogWarning("Attempt to generate recovery codes for user with 2FA disabled.");
                return RedirectToAction(nameof(Index), "Manage", new { Message = ManageMessageId.EnableAuthenticator });
            }

            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 5);
            _logger.LogInformation("User {UserId} generated new recovery codes.", user.Id);

            return View("DisplayRecoveryCodes", new DisplayRecoveryCodesViewModel { Codes = codes! });
        }

        [HttpGet]
        public async Task<IActionResult> EditProfileAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return View();

            var model = new EditExtraProfileModel()
            {
                BirthDate = user.BirthDate,
                HomeAddress = user.HomeAddress,
                UserName = user.UserName,
                UserEmail = user.Email,
                PhoneNumber = user.PhoneNumber,
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfileAsync(EditExtraProfileModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return View();

            user.HomeAddress = model.HomeAddress;
            user.BirthDate = model.BirthDate;
            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index), "Manage");
        }
    }
}
