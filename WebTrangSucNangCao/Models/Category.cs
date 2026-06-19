using System;
using System.Collections.Generic;

namespace WebTrangSucNangCao.Models;

public partial class Category
{
    public int Id { get; set; }

    public string Idcate { get; set; } = null!;

    public string? NameCate { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
