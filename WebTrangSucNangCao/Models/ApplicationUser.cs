using Microsoft.AspNetCore.Identity;

namespace WebTrangSucNangCao.Models
{
    // Kế thừa IdentityUser để lấy Email, Password, SĐT mặc định
    public class ApplicationUser : IdentityUser
    {
        // Khai báo thêm mấy cột mà View mày đang gọi
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
    }
}