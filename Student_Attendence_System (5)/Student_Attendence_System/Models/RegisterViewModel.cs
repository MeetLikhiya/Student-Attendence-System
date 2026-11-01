using System.ComponentModel.DataAnnotations;

namespace Student_Attendence_System.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Enrollment No")]
        public string EnrollmentNo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Student Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Class")]
        public string? ClassName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}