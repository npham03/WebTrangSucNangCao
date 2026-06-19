using System;
using System.Collections.Generic;

namespace WebTrangSucNangCao.Models;

public partial class Customer
{
    public int Idcus { get; set; }

    public string? NameCus { get; set; }

    public string? PhoneCus { get; set; }

    public string? EmailCus { get; set; }

    public virtual ICollection<OrderPro> OrderPros { get; set; } = new List<OrderPro>();
}
