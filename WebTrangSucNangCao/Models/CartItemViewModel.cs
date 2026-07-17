using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using WebTrangSucNangCao.Models;

namespace WebTrangSucNangCao.Models
{
    // Class này KHÔNG liên quan đến DB, chỉ dùng cho Session
    public class CartItemViewModel
    {
        // Info từ Variant
        public int VariantId { get; set; }
        public string? VariantName { get; set; }

        // Info từ Product
        public int ProductId { get; set; }
        public string? NamePro { get; set; }
        public string? ImagePro { get; set; }
        public decimal Price { get; set; }

        // Info Giỏ hàng
        public int Quantity { get; set; }
    }
}