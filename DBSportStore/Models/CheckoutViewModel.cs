using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DBSportStore.Models
{
    public class CheckoutViewModel
    {
        [Required(
            ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required(
            ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(
            ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(
            ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(
            ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [StringLength(250)]
        [Display(Name = "Địa chỉ khách hàng")]
        public string Address { get; set; }

        [Required(
            ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(250)]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        // Cart lấy từ Session để View hiển thị tóm tắt đơn.
        // Không phải trường người dùng nhập.
        public Cart Cart { get; set; }

    }
}
