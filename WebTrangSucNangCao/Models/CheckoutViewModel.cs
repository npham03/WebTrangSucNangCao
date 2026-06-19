using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WebTrangSucNangCao.Models
{
    public class CheckoutViewModel
    {
        // Phần thông tin giao hàng (sẽ tự điền)
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Address { get; set; }

        // Phần giỏ hàng
        public List<CartItemViewModel> CartItems { get; set; }
        public decimal GrandTotal { get; set; }
    }
}