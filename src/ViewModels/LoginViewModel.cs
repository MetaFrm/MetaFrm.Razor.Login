using MetaFrm.MVVM;
using System.ComponentModel.DataAnnotations;

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
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [Required]
        [MinLength(6)]
        public string? Password { get; set; }
    }
}