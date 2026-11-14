
namespace WebPortal_OH_FI.Models
{
    public class UploadParametersModel
    {
        public int DeleteAfterDays { get; set; }
        public string DisplayAllowedSize { get; set; }
        public int AllowedSize { get; set; }
        public string AllowedExtensions { get; set; }
        public string ExtensionFormats { get; set; }
    }
}