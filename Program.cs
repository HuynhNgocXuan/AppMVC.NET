using System.Net;
using Microsoft.AspNetCore.Mvc.Razor;
using webMVC.Services;
using webMVC.ExtendMethods;
using webMVC.Models;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);









builder.Logging.ClearProviders(); // Xóa các provider mặc định nếu cần
builder.Logging.AddConsole(); //

#pragma warning disable ASP0011 // Suggest using builder.Logging over Host.ConfigureLogging or WebHost.ConfigureLogging
builder.Host.ConfigureLogging(logging =>
{
    logging.AddConsole();
});
#pragma warning restore ASP0011 // Suggest using builder.Logging over Host.ConfigureLogging or WebHost.ConfigureLogging

builder.Services.AddDbContext<AppDbContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppMvcConnectionString"))
        .EnableSensitiveDataLogging() // Log thêm thông tin nhạy cảm (cân nhắc chỉ bật trong môi trường phát triển)
        .EnableDetailedErrors();     // Log chi tiết lỗi EF Core (chỉ nên bật trong môi trường phát triển)
});

// builder.Services.AddIdentity<IdentityUser, IdentityRole>()
//     .AddEntityFrameworkStores<AppDbContext>()
//     .AddDefaultTokenProviders();

// Add services to the container.






builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

builder.Services.Configure<RazorViewEngineOptions>(option => {
    option.ViewLocationFormats.Add("/MyView/{1}/{0}" + RazorViewEngine.ViewExtension);
});

builder.Services.AddSingleton<ProductService>();

builder.Services.AddSingleton<PlanetService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.AddStatusCodePage(); // Tùy biến

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();


// app.MapGet("/sayhi", async context => 
// {
//     await context.Response.WriteAsync("Hello");
// }); // dùng cho các ứng dụng nhỏ 

// app.MapControllerRoute(
//     name: "route1",
//     pattern: "say-hi",
//     defaults: new
//     {
//         controller = "First",
//         action = "ProductView",
//         id = 2
//     }
// );

// app.MapControllerRoute(
//     name: "route2",
//     pattern: "{url}/{id?}",
//     defaults: new
//     {
//         controller = "First",
//         action = "ProductView",
//     }
// );

app.MapRazorPages();

app.MapControllers();

app.MapAreaControllerRoute(
    name: "areasData",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    areaName: "DataBase"
);
   


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets(); // hoặc app.UseStaticFiles();

app.Run();
