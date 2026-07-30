using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebTrangSucNangCao.Models
{
    public class CheckoutViewModel
    {
        // Thông tin người nhận cơ bản
        [Required(ErrorMessage = "Vui lòng nhập Họ và Tên người nhận.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không đúng định dạng.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng (Ví dụ: example@gmail.com).")]
        public string Email { get; set; } = string.Empty;

        // Các trường địa chỉ chi tiết (đã sửa để khớp hoàn toàn với View)
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;

        // Các trường phục vụ tính phí ship, coupon và thanh toán
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal ShippingFee { get; set; } = 0;
        public decimal TotalPayment { get; set; } = 0;

        // Giỏ hàng hiện tại
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public decimal CartSubtotal { get; set; } = 0;

        public string Address { get; set; } = string.Empty;
    }
}