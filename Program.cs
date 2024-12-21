using System.Net;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Configuration;
using webMVC.ExtendMethods;
using webMVC.Data;
using webMVC.Services;
using webMVC.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;


var builder = WebApplication.CreateBuilder(args);


builder.Logging.ClearProviders();

builder.Logging.AddConsole();
//

#pragma warning disable ASP0011
builder.Host.ConfigureLogging(logging =>
{
    logging.AddConsole();
});


builder.Services.AddOptions();


builder.Services.Configure<EncryptionSettingsModel>(builder.Configuration.GetSection("EncryptionSettings"));
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();


builder.Services.AddScoped<IQRCodeService, QRCodeService>();


var mailSetting = builder.Configuration.GetSection("MailSettings");
builder.Services.Configure<MailSettings>(mailSetting);
builder.Services.AddSingleton<IEmailSender, SendMailService>();
builder.Services.AddSingleton<ISmsSender, SendSmsService>();


builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppMvcConnectionString"))
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors();
});

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

builder.Services.AddOptions();

builder.Services.Configure<RazorViewEngineOptions>(option =>
{
    // {0} -> ten Action
    // {1} -> ten Controller
    // {2} -> ten Area
    option.ViewLocationFormats.Add("/MyView/{1}/{0}" + RazorViewEngine.ViewExtension);
    option.AreaViewLocationFormats.Add("/MyAreas/{2}/Views/{1}/{0}.cshtml");
});

builder.Services.AddSingleton<ProductService>();

builder.Services.AddSingleton<PlanetService>();


builder.Services.AddIdentity<AppUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();


builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.SignIn.RequireConfirmedAccount = true;

    options.Stores.ProtectPersonalData = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login/";
    options.LogoutPath = "/logout/";
    options.AccessDeniedPath = "/khongduoctruycap.html";
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(300);
});

builder.Services.AddAuthentication()
   .AddGoogle(options =>
   {
       var googleConfig = builder.Configuration.GetSection("Authentication:Google");
       options.ClientId = googleConfig["ClientId"] ?? throw new InvalidOperationException("Google configuration section is missing ClientId.");
       options.ClientSecret = googleConfig["ClientSecret"] ?? throw new InvalidOperationException("Google configuration section is missing ClientSecret.");
       // https://localhost:5001/signin-google
       options.CallbackPath = "/dang-nhap-tu-google";
   })
   .AddFacebook(options =>
   {
       var facebookConfig = builder.Configuration.GetSection("Authentication:Facebook");
       options.AppId = facebookConfig["AppId"] ?? throw new InvalidOperationException("Facebook configuration section is missing AppId.");
       options.AppSecret = facebookConfig["AppSecret"] ?? throw new InvalidOperationException("Facebook configuration section is missing AppSecret.");
       options.CallbackPath = "/dang-nhap-tu-facebook";
   })
    .AddCookie(options =>
    {

        options.SlidingExpiration = false;
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
//  .AddTwitter()
//  .AddMicrosoftAccount()
;


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<IdentityErrorDescriber, AppIdentityErrorDescriber>();


builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicyProvider.AddCustomPolicies(options);
});


builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;

    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});





var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.AddStatusCodePage();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider
    (
        Path.Combine(Directory.GetCurrentDirectory(), "Uploads")
    ),
    RequestPath = "/contents"
});

app.UseRouting();

app.UseSession();

app.UseCookiePolicy();

app.UseAuthentication();

app.UseAuthorization();




app.MapRazorPages();

app.MapControllers();

app.MapAreaControllerRoute(
    name: "areasData",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    areaName: "DataBase"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// .WithStaticAssets();
// hoặc app.UseStaticFiles();

app.Run();
