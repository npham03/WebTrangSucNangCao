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
        [Required(ErrorMessage = "Vui lòng nhập Họ và Tên")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ giao hàng")]
        public string? Address { get; set; }

        // Phần giỏ hàng
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public List<CartItemViewModel>? CartItems { get; set; } = new List<CartItemViewModel>();

        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public decimal GrandTotal { get; set; }
    }
}