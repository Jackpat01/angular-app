
namespace WebPortal_OH_FI.Models
{
    /// <summary>
    /// This class controls all the objects returned to Pages as JSON Results.
    /// </summary>
    public static class ReturnJson
    {
        /// <summary>
        /// 
        /// </summary>
        public class Info
        {
            /// <summary>
            /// Gets or sets the success.
            /// </summary>
            /// <value>
            /// The success.
            /// </value>
            public bool Success { get; set; }
            /// <summary>
            /// Gets or sets the message.
            /// </summary>
            /// <value>
            /// The message.
            /// </value>
            public string Message { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="EditInfo"/> class.
            /// </summary>
            public Info()
            {

            }
            /// <summary>
            /// Initializes a new instance of the <see cref="EditInfo"/> class.
            /// </summary>
            /// <param name="_success">The _success.</param>
            /// <param name="_message">The _message.</param>
            public Info(bool _success, string _message)
            {
                Success = _success;
                Message = _message;
            }
        }
    }
}