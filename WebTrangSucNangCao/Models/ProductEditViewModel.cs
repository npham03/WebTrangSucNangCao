using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebTrangSucNangCao.Models
{
    public class ProductEditViewModel
    {
        // 1. Thông tin sản phẩm (lấy từ DB)
        public Product Product { get; set; }

        // 2. List ảnh gallery (lấy từ DB)
        public List<ProductImage> Images { get; set; }

        // 3. List size (lấy từ DB)
        public List<ProductVariant> Variants { get; set; }

        // 4. List category (dùng cho dropdown)
        public SelectList CategoryList { get; set; }
    }
}