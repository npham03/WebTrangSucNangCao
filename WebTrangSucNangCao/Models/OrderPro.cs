using System;
using System.Collections.Generic;

namespace WebTrangSucNangCao.Models;

public partial class OrderPro
{
    public int Id { get; set; }

    public DateOnly? DateOrder { get; set; }

    public int? Idcus { get; set; }

    public string? AddressDeliverry { get; set; }

    public string? Status { get; set; }

    public virtual Customer? IdcusNavigation { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
