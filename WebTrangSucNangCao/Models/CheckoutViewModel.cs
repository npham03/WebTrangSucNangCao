using System.Collections.Generic;

namespace WebTrangSucNangCao.Models
{
    public class CheckoutViewModel
    {
        // Thông tin người nhận cơ bản
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
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