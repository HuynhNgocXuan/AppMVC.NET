// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace webMVC.Areas.Identity.Models.AccountViewModels
{
    public class VerifyCodeViewModel
    {
        [Required(ErrorMessage = "Không có nhà cung cấp!")]
        public string? Provider { get; set; }


        [DisplayName("Code")]
        [Required(ErrorMessage = "Phải nhập {0}")]
        public string? Code { get; set; }

        public string? ReturnUrl { get; set; }

        [Display(Name = "Nhớ cho trình duyệt này?")]
        public bool RememberBrowser { get; set; }

        [Display(Name = "Nhớ thông tin đăng nhập?")]
        public bool RememberMe { get; set; }
    }
}
