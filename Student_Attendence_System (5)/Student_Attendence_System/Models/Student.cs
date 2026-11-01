using System.ComponentModel.DataAnnotations;

namespace Student_Attendence_System.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Enrollment No is required")]
        [Display(Name = "Enrollment No")]
        public string EnrollmentNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student name is required")]
        [Display(Name = "Student Name")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Enter a valid email")]
        public string? Email { get; set; }

        [Display(Name = "Class")]
        public string? ClassName { get; set; }

        [Display(Name = "Face Data")]
        public string? FaceData { get; set; }

        [Display(Name = "Face Image")]
        public string? FaceImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public ICollection<Attendance> Attendances { get; set; }
            = new List<Attendance>();
    }
}