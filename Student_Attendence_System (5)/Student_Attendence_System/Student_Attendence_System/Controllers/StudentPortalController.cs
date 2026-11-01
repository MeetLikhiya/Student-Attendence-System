using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendence_System.Data;
using Student_Attendence_System.Models;

namespace Student_Attendence_System.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentPortalController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null || user.StudentId == null)
            {
                return Forbid();
            }

            var student =
                await _context.Students
                    .FirstOrDefaultAsync(
                        s => s.Id == user.StudentId.Value);

            if (student == null)
            {
                return NotFound();
            }

            var attendance =
                await _context.Attendances
                    .Where(a => a.StudentId == student.Id)
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenByDescending(a => a.AttendanceTime)
                    .ToListAsync();

            ViewBag.Student = student;

            ViewBag.TotalDays =
                attendance.Count;

            ViewBag.PresentDays =
                attendance.Count(
                    a => a.Status == "Present");

            return View(attendance);
        }
    }
}