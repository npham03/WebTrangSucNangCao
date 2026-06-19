using System;
using System.Collections.Generic;

namespace WebTrangSucNangCao.Models;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int? Idproduct { get; set; }

    public int? Idorder { get; set; }

    public int? Quantity { get; set; }

    public double? UnitPrice { get; set; }

    public int? VariantId { get; set; }

    public virtual OrderPro? IdorderNavigation { get; set; }

    public virtual Product? IdproductNavigation { get; set; }

    public virtual ProductVariant? Variant { get; set; }
}
