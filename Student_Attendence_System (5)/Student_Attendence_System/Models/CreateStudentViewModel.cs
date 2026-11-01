using System.ComponentModel.DataAnnotations;

namespace YourProjectName.Models
{
    public class CreateStudentViewModel
    {
        [Required]
        public string EnrollmentNo { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? ClassName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}