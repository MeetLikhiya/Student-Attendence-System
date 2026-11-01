using Microsoft.AspNetCore.Identity;

namespace Student_Attendence_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? StudentId { get; set; }
    }
}