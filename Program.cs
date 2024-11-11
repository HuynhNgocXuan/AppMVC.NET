using System.Net;
using Microsoft.AspNetCore.Mvc.Razor;
using webMVC.Services;
using webMVC.ExtendMethods;


var builder = WebApplication.CreateBuilder(args);


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
    name: "areas",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    areaName: "ProductManager"
    );
    
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets(); // hoặc app.UseStaticFiles();

app.Run();
