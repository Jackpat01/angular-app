using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using WebPortal_OH_FI.BL;

namespace WebPortal_OH_FI.Models
{
    /// <summary>
    /// Home View Model
    /// </summary>
    public class HomeViewModel
    {
        /// <summary>
        /// Gets or sets the non hashed password.
        /// </summary>
        /// <value>
        /// The non hashed password.
        /// </value>
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Password is required.")]
        public string NonHashedPassword { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public class EULA
        {
            /// <summary>
            /// Gets or sets the EULA text.
            /// </summary>
            /// <value>
            /// The EULA text.
            /// </value>
            [AllowHtml]
            public string EULAText { get; set; }

            /// <summary>
            /// Gets or sets the Email identifier.
            /// </summary>
            /// <value>
            /// The Email identifier.
            /// </value>
            public string Email { get; set; }
        }

        /// <summary>
        /// Change Password View Model
        /// </summary>
        public class ChangePasswordModel
        {
            /// <summary>
            /// Gets or sets the email.
            /// </summary>
            /// <value>
            /// The email.
            /// </value>
            public string Email { get; set; }

            [Required(ErrorMessage = "Current Password required.")]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; }

            /// <summary>
            /// Gets or sets the password.
            /// </summary>
            /// <value>
            /// The password.
            /// </value>
            [Required(ErrorMessage = "New Password is required.")]
            [DataType(DataType.Password)]
            [RegularExpression(@"^.*(?=.*[!@#$%^&*\(\)_\-+=]).*$",
                ErrorMessage =
                    "Password not strong enough, use one of the following (!, @, #, $, %, ^, &, -, +, =, _, *) special characters.")]
            [StringLength(int.MaxValue, MinimumLength = 8, ErrorMessage = "Password is at least 8 characters.")]
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets the confirm password.
            /// </summary>
            /// <value>
            /// The confirm password.
            /// </value>
            [Required(ErrorMessage = "New Password Confirm is required.")]
            [DataType(DataType.Password)]
            [RegularExpression(@"^.*(?=.*[!@#$%^&*\(\)_\-+=]).*$",
                ErrorMessage =
                    "Password not strong enough, use one of the following (!, @, #, $, %, ^, &, -, +, =, _, *) special characters.")]
            [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Password doesn't match.")]
            public string ConfirmPassword { get; set; }
        }
    }
}