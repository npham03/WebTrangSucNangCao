using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebTrangSucNangCao.Models; // Sửa theo namespace của bạn
using WebTrangSucNangCao.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ Gửi Email Resend API
builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>();

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

// THÊM VÀO: Cấu hình đăng nhập bằng Google & Facebook
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        // Nhập ClientId và ClientSecret của bạn sau khi tạo trên Google Cloud Console
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "YOUR_GOOGLE_CLIENT_ID";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
    })
    .AddFacebook(options =>
    {
        // Nhập AppId và AppSecret của bạn sau khi tạo trên Facebook Developers
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "YOUR_FACEBOOK_APP_ID";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "YOUR_FACEBOOK_APP_SECRET";
    });

// Cấu hình Session cho giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình Đa ngôn ngữ (Localization)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
var app = builder.Build();

// Sử dụng Session
app.UseSession();
// Cấu hình MIME types cho file 3D model (.glb, .gltf)
var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".glb"] = "model/gltf-binary";
contentTypeProvider.Mappings[".gltf"] = "model/gltf+json";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider
});

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


app.UseRouting();
// Thêm cấu hình ngôn ngữ mặc định và danh sách hỗ trợ
var supportedCultures = new[] { "en", "vi" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0]) // Mặc định là tiếng Anh (en)
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();