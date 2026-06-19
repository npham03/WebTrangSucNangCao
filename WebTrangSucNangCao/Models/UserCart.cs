using System;
using System.Collections.Generic;

namespace WebTrangSucNangCao.Models;

public partial class UserCart
{
    public int UserCartId { get; set; }

    public string? UserId { get; set; }

    public int? Quantity { get; set; }

    public int VariantId { get; set; }

    public virtual ProductVariant Variant { get; set; } = null!;
}
