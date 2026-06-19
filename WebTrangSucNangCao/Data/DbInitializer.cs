using Microsoft.AspNetCore.Identity;

namespace WebTrangSucNangCao.Models // Sửa lại namespace cho đúng với project của mày
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            // Lấy 2 thằng quản lý Role và User ra
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(); // Nhớ đổi IdentityUser thành ApplicationUser nếu mày xài class kế thừa

            // 1. Kiểm tra và tạo Role "Admin"
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // 2. Kiểm tra và tạo Tài khoản Admin
            string adminEmail = "admin@rosaria.com"; // Email đăng nhập
            string adminPassword = "Password@123";   // Pass bắt buộc phải có Hoa, thường, số, ký tự đặc biệt

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser // Đổi thành ApplicationUser nếu cần
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true // Tự động xác thực email để log in được luôn
                };

                var createPowerUser = await userManager.CreateAsync(newAdmin, adminPassword);

                if (createPowerUser.Succeeded)
                {
                    // Gán cái Role Admin cho thằng tài khoản vừa tạo
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
            // ... (code cũ)
            // 1. Kiểm tra và tạo Role "Admin"
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // ---------------------------------------------------------
            // THÊM ĐOẠN NÀY VÀO ĐỂ NÓ TỰ ĐỘNG TẠO ROLE CUSTOMER:
            if (!await roleManager.RoleExistsAsync("Customer"))
            {
                await roleManager.CreateAsync(new IdentityRole("Customer"));
            }
            // ---------------------------------------------------------
        }
    }
}