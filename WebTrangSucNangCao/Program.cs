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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();