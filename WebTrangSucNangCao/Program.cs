using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebTrangSucNangCao.Models; // Sửa theo namespace của bạn

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ShopOnlineContext>(options => options.UseSqlServer(connectionString)); // Context sinh ra từ lệnh Scaffold

// Cấu hình Identity (Role và User)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ShopOnlineContext>()
.AddDefaultTokenProviders();

// Cấu hình Session cho giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Sử dụng Session
app.UseSession();
app.UseStaticFiles();

// Backward compatibility: Hỗ trợ tự động map các đường dẫn ảnh cũ (imgages, imagesProducts, Content/Img) vào thư mục images chuẩn
var imagesPath = Path.Combine(builder.Environment.WebRootPath, "images");
if (Directory.Exists(imagesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagesPath),
        RequestPath = "/imgages"
    });
}
var productsPath = Path.Combine(builder.Environment.WebRootPath, "images", "products");
if (Directory.Exists(productsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(productsPath),
        RequestPath = "/imagesProducts"
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(productsPath),
        RequestPath = "/Content/Img/Products"
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(productsPath),
        RequestPath = "/Content/Img"
    });
}
var bannersPath = Path.Combine(builder.Environment.WebRootPath, "images", "banners");
if (Directory.Exists(bannersPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(bannersPath),
        RequestPath = "/imagesBanners"
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(bannersPath),
        RequestPath = "/Content/Img/Banners"
    });
}

app.UseMiddleware<WebTrangSucNangCao.Models.PerformanceLogMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();