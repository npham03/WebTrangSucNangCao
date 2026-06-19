using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace WebTrangSucNangCao.Models
{
    public class ProductHomeViewModel
    {
        // Thông tin sản phẩm (Tên, Giá, ID...)
        public Product Product { get; set; }

        // ẢNH THỨ 2 (để hover)
        public string HoverImage { get; set; }
    }
}