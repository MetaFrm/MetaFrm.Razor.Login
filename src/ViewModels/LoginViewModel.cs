﻿using MetaFrm.MVVM;
using MetaFrm.Razor.Essentials.ComponentModel.DataAnnotations;
using DisplayAttribute = System.ComponentModel.DataAnnotations.DisplayAttribute;

namespace MetaFrm.Razor.ViewModels
{
    /// <summary>
    /// LoginViewModel
    /// </summary>
    public partial class LoginViewModel : BaseViewModel
    {
        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "이메일")]
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [Display(Name = "비밀번호")]
        [Required]
        [MinLength(6)]
        public string? Password { get; set; }
    }
}