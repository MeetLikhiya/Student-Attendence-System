using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendence_System.Data;

namespace Student_Attendence_System.Controllers
{
    [Authorize(Roles = "Faculty")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            ViewBag.TotalStudents =
                await _context.Students.CountAsync();

            ViewBag.TodayPresent =
                await _context.Attendances
                    .CountAsync(a =>
                        a.AttendanceDate == today &&
                        a.Status == "Present");

            ViewBag.TotalAttendance =
                await _context.Attendances.CountAsync();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}