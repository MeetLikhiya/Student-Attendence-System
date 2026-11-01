using System.ComponentModel.DataAnnotations;

namespace Student_Attendence_System.Models
{
    public class StudentAccountViewModel
    {
        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string EnrollmentNo { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}