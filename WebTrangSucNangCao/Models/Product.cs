using System;
using System.Collections.Generic;

namespace WebTrangSucNangCao.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string? NamePro { get; set; }

    public string? DecriptionPro { get; set; }

    public string? Category { get; set; }

    public decimal? Price { get; set; }

    public string? ImagePro { get; set; }

    public string? Material { get; set; }

    public virtual Category? CategoryNavigation { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
