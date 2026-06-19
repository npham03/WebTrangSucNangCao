using System.Collections.Generic;

namespace WebTrangSucNangCao.Models
{
    public class AdminDashboardViewModel
    {
        // Dùng cho List Slider và Carousel
        public List<Slider> Sliders { get; set; }

        // Dùng cho Form "Thêm Slider"
        public Slider NewSlider { get; set; }

        // Dùng cho List Sản Phẩm
        public List<Product> Products { get; set; }
        // 1. Tổng doanh thu (đã bán thành công)
        public decimal TotalRevenue { get; set; }

        // 2. Số đơn hàng đang chờ xử lý
        public int PendingOrdersCount { get; set; }

        // 3. Tổng số lượng sản phẩm đã bán ra
        public int TotalProductsSold { get; set; }

        // 4. Danh sách sản phẩm sắp hết hàng (Ví dụ: Tồn kho < 5)
        public List<ProductVariant> LowStockVariants { get; set; }
    }
}