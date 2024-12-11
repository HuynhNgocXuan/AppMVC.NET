// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using webMVC.Areas.Identity.Models.AccountViewModels;
using webMVC.ExtendMethods;
using webMVC.Models;
using webMVC.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using webMVC.Services;

namespace webMVC.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("/Account/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger<AccountController> _logger;


        [TempData]
        public string? StatusMessage { get; set; }

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            ISmsSender smsSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet("/login/")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            if (User.Identity == null) return View();  

            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl);
            }
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost("/login/")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
                return View(model);
            }

            if (string.IsNullOrEmpty(model?.UserNameOrEmail))
            {
                ModelState.AddModelError(string.Empty, "Email hoặc tên người dùng không được để trống.");
                return View(model);
            }
            var user = AppUtilities.IsValidEmail(model.UserNameOrEmail)
                ? await _userManager.FindByEmailAsync(model.UserNameOrEmail)
                : await _userManager.FindByNameAsync(model.UserNameOrEmail);


            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Email chưa được xác nhận.");
                return View(model);
            }

            if (user.UserName == null)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Đăng nhập thành công.");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Tài khoản bị khóa.");
                return View("Lockout");
            }

            ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không chính xác.");
            return View(model);
        }



        // POST: /Account/LogOff
        [HttpPost("/logout/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User đăng xuất");
            StatusMessage = "Bạn đã đăng xuất!";
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            if (User.Identity!.IsAuthenticated)
            {
                return LocalRedirect(returnUrl);
            }
            return View();
        }
        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new AppUser { UserName = model.UserName, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Đã tạo user mới.");

                    // Phát sinh token để xác nhận email
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    // https://localhost:5001/confirm-email?userId=fdsfds&code=xyz&returnUrl=
                    var callBackUrl = Url.ActionLink(
                        action: nameof(ConfirmEmail),
                        values:
                            new
                            {
                                area = "Identity",
                                userId = user.Id,
                                code
                            },
                        protocol: Request.Scheme);

                    if (callBackUrl == null) return View();

                    string stringHtml = @$"Bạn đã đăng ký tài khoản trên webMVC, hãy <a href='{HtmlEncoder.Default.Encode(callBackUrl)}'>bấm vào đây</a> để kích hoạt tài khoản.";
                    string stringSubject = "Xác nhận địa chỉ email";
                    await _emailSender.SendEmailAsync(model.Email, stringSubject, stringHtml);

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        var redirectUrl = Url.Action(nameof(RegisterConfirmation));

                        if (redirectUrl == null)
                        {
                            _logger.LogError("The action URL for RegisterConfirmation could not be generated.");
                            return RedirectToAction("Index", "Home", new { area = "" });
                        }
                        return LocalRedirect(redirectUrl);
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                ModelState.AddModelError(result);
            }
            return View(model);
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("ErrorConfirmEmail");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return View("ErrorConfirmEmail");

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogError($"Error confirming email: {error.Description}");
                }
            }
            return View(result.Succeeded ? "ConfirmEmail" : "ErrorConfirmEmail");
        }

        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // GET: /Account/ExternalLoginCallback  
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi sử dụng dịch vụ ngoài: {remoteError}");
                return View(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                // Cập nhật lại token
                await _signInManager.UpdateExternalAuthenticationTokensAsync(info);

                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["ProviderDisplayName"] = info.ProviderDisplayName;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email == null) return View();
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }


        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    _logger.LogWarning("External login info is null.");
                    return View("ExternalLoginFailure");
                }

                string? externalEmail = null;
                AppUser? externalEmailUser = null;

                // Lấy email từ dịch vụ ngoài (và kiểm tra xem có hợp lệ không)
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    externalEmail = info.Principal.FindFirstValue(ClaimTypes.Email);
                    if (string.IsNullOrWhiteSpace(externalEmail))
                    {
                        ModelState.AddModelError(string.Empty, "Dịch vụ xác thực ngoài không cung cấp email hợp lệ.");
                        return View(model);
                    }
                    externalEmailUser = await _userManager.FindByEmailAsync(externalEmail);
                }

                // Tìm người dùng đã đăng ký qua email được cung cấp
                var registeredUser = await _userManager.FindByEmailAsync(model.Email);

                // Trường hợp cả email từ dịch vụ ngoài và email từ người dùng nhập đều tồn tại
                if (registeredUser != null && externalEmailUser != null)
                {
                    if (registeredUser.Id == externalEmailUser.Id)
                    {
                        // Người dùng đã liên kết, thực hiện liên kết thông tin đăng nhập
                        var resultLink = await _userManager.AddLoginAsync(registeredUser, info);
                        if (resultLink.Succeeded)
                        {
                            await _signInManager.SignInAsync(registeredUser, isPersistent: false);
                            _logger.LogInformation("Tài khoản đã liên kết thành công với dịch vụ ngoài.");
                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Không thể liên kết tài khoản với dịch vụ ngoài.");
                            return View(model);
                        }
                    }
                    else
                    {
                        // Email từ dịch vụ ngoài không khớp với tài khoản người dùng
                        ModelState.AddModelError(string.Empty, "Không thể liên kết tài khoản. Vui lòng sử dụng email khác.");
                        return View(model);
                    }
                }

                // Trường hợp email từ dịch vụ ngoài đã có trong hệ thống, nhưng không khớp với email nhập vào
                if (externalEmailUser != null && registeredUser == null)
                {
                    ModelState.AddModelError(string.Empty, "Email từ dịch vụ ngoài đã tồn tại. Không thể tạo tài khoản mới với email khác.");
                    return View(model);
                }

                // Trường hợp email từ dịch vụ ngoài khớp với email nhập vào nhưng chưa có tài khoản
                if (externalEmailUser == null && externalEmail == model.Email)
                {
                    var newUser = new AppUser()
                    {
                        UserName = externalEmail,
                        Email = externalEmail
                    };

                    var resultNewUser = await _userManager.CreateAsync(newUser);
                    if (resultNewUser.Succeeded)
                    {
                        await _userManager.AddLoginAsync(newUser, info);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                        await _userManager.ConfirmEmailAsync(newUser, code);

                        await _signInManager.SignInAsync(newUser, isPersistent: false);

                        _logger.LogInformation("Tài khoản mới đã được tạo và liên kết với dịch vụ ngoài.");
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản mới. Vui lòng thử lại.");
                        _logger.LogError("Lỗi khi tạo tài khoản mới: {Errors}", resultNewUser.Errors);
                        return View(model);
                    }
                }

                // Trường hợp tạo tài khoản mới thông qua thông tin nhập vào
                if (externalEmailUser == null && registeredUser == null)
                {
                    var user = new AppUser { UserName = model.Email, Email = model.Email };
                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        result = await _userManager.AddLoginAsync(user, info);
                        if (result.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            _logger.LogInformation("Người dùng đã tạo tài khoản mới bằng dịch vụ ngoài: {Provider}", info.LoginProvider);

                            // Cập nhật các token xác thực ngoài
                            await _signInManager.UpdateExternalAuthenticationTokensAsync(info);

                            return LocalRedirect(returnUrl);
                        }
                    }

                    ModelState.AddModelError(string.Empty, "Lỗi khi tạo tài khoản mới hoặc liên kết với dịch vụ ngoài.");
                    _logger.LogError("Lỗi khi tạo tài khoản: {Errors}", result.Errors);
                }
            }

            // Trả lại view nếu có lỗi
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }



        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    StatusMessage = "Tài khoản không tồn tại hoặc chưa xác thực email!";
                    return View("ForgotPasswordConfirmation");
                }
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.ActionLink(
                    action: nameof(ResetPassword),
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);


                if (callbackUrl == null) return View();

                var stringHtml = $"Hãy bấm <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>vào đây</a> để đặt lại mật khẩu.";
                await _emailSender.SendEmailAsync(model.Email, "Reset Password", stringHtml);

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? code = null)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }

            string code;
            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Mã xác thực không hợp lệ.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string? returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }
        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid) return View();

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return View("Error");

            if (model.SelectedProvider == null) return View("Error");

            if (model.SelectedProvider == "Authenticator")
            {
                return RedirectToAction(nameof(VerifyAuthenticatorCode), new { ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
            }

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code)) return View("Error");


            var message = "Your security code is: " + code;


            var email = await _userManager.GetEmailAsync(user);
            if (email == null) return View();
            if (model.SelectedProvider == "Email")
                await _emailSender.SendEmailAsync(email, "Security Code", message);


            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (phoneNumber == null) return View();
            else if (model.SelectedProvider == "Phone")
                await _smsSender.SendSmsAsync(phoneNumber, message);

            return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }
        //
        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            if (returnUrl == null) return View();

            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError($"Error: {error.ErrorMessage}");
                    ModelState.AddModelError(string.Empty, "The data provided is invalid." + error.ErrorMessage);
                }
                return View(model);
            }

            try
            {
                var result = await _signInManager.TwoFactorSignInAsync(
                    model.Provider!,
                    model.Code!,
                    model.RememberMe,
                    model.RememberBrowser);

                if (result.Succeeded)
                {
                    _logger.LogInformation("2FA sign-in succeeded for user.");

                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        _logger.LogInformation("Redirecting to local ReturnUrl: {ReturnUrl}", model.ReturnUrl);
                        return LocalRedirect(model.ReturnUrl);
                    }
                    else
                    {
                        _logger.LogWarning("ReturnUrl is not local. Redirecting to default home.");
                        return RedirectToAction("Index", "Home");
                    }
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning(7, "User account locked out.");
                    return View("Lockout");
                }

                _logger.LogWarning("Invalid 2FA code entered for provider: {Provider}", model.Provider);
                ModelState.AddModelError(string.Empty, "Invalid verification code. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during 2FA verification.");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
            }

            _logger.LogDebug("Returning view with error model state.");
            return View(model);
        }



        //
        // GET: /Account/VerifyAuthenticatorCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAuthenticatorCode(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyAuthenticatorCodeViewModel { ReturnUrl = returnUrl!, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyAuthenticatorCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAuthenticatorCode(VerifyAuthenticatorCodeViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid) 
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError($"Error: {error.ErrorMessage}");
                    ModelState.AddModelError(string.Empty, "The data provided is invalid." + error.ErrorMessage);
                }
                return View(model);
            }

            try
            {
                var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                    model.Code!,
                    model.RememberMe,
                    model.RememberBrowser
                );

                if (result.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Xác thực thành công!");
                    return LocalRedirect(model.ReturnUrl);
                }

                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }

                ModelState.AddModelError(string.Empty, "Mã xác thực không hợp lệ. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình xác minh mã xác thực.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
            }

            return View(model);
        }


        //
        // GET: /Account/UseRecoveryCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> UseRecoveryCode(string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new UseRecoveryCodeViewModel { ReturnUrl = returnUrl! });
        }

        //
        // POST: /Account/UseRecoveryCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseRecoveryCode(UseRecoveryCodeViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError($"ModelState Error: {error.ErrorMessage}");
                }
                ModelState.AddModelError(string.Empty, "Dữ liệu nhập vào không hợp lệ.");
                return View(model);
            }

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(model.Code!);

            if (result.Succeeded)
            {
                return LocalRedirect(model.ReturnUrl);
            }

            _logger.LogWarning("Sai mã phục hồi.");
            ModelState.AddModelError(string.Empty, "Sai mã phục hồi. Vui lòng kiểm tra lại.");
            return View(model);
        }


        [Route("/khongduoctruycap.html")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
