using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
namespace WebTrangSucNangCao.Models
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<ProductImage> Images { get; set; }
        public List<ProductVariant> Variants { get; set; }
        public List<Product> RelatedProducts { get; set; }
    }
}