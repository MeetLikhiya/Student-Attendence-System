using System.ComponentModel.DataAnnotations;

namespace Student_Attendence_System.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        [Required(ErrorMessage = "Please select a role.")]
        public string Role { get; set; } = string.Empty;
    }
}