using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendence_System;
using Student_Attendence_System.Data;
using Student_Attendence_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Student_Attendence_System.Controllers
{
    [Authorize(Roles = "Faculty")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FaceRecognitionClient _faceRecognitionClient;

        public AttendanceController(
            ApplicationDbContext context,
            FaceRecognitionClient faceRecognitionClient)
        {
            _context = context;
            _faceRecognitionClient = faceRecognitionClient;
        }


        // ============================================
        // ATTENDANCE LIST
        // ============================================

        public async Task<IActionResult> Index()
        {
            var attendance =
                await _context.Attendances
                    .Include(a => a.Student)
                    .OrderByDescending(
                        a => a.AttendanceDate)
                    .ThenByDescending(
                        a => a.AttendanceTime)
                    .ToListAsync();

            return View(attendance);
        }


        // ============================================
        // ATTENDANCE CAMERA PAGE
        // ============================================

        public IActionResult Mark()
        {
            return View();
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Recognize(
            IFormFile faceImage)
        {
            if (faceImage == null ||
                faceImage.Length == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No face image was received."
                });
            }


            // ============================================
            // READ CAPTURED IMAGE
            // ============================================

            byte[] imageBytes;

            using (var memoryStream =
                   new MemoryStream())
            {
                await faceImage.CopyToAsync(
                    memoryStream);

                imageBytes =
                    memoryStream.ToArray();
            }


            // ============================================
            // LOCAL FACE RECOGNITION
            // ============================================

            int? matchedStudentId;

            try
            {
                matchedStudentId =
                    await _faceRecognitionClient
                        .RecognizeAsync(imageBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return Json(new
                {
                    success = false,
                    message =
                        "Recognition error: " +
                        ex.Message
                });
            }


            // ============================================
            // NO MATCH
            // ============================================

            if (matchedStudentId == null)
            {
                return Json(new
                {
                    success = false,
                    matched = false,
                    message =
                        "Face not recognized. " +
                        "Attendance was NOT marked."
                });
            }


            // ============================================
            // FIND MATCHED STUDENT
            // ============================================

            var student =
                await _context.Students
                    .FirstOrDefaultAsync(
                        s => s.Id ==
                             matchedStudentId.Value);


            if (student == null)
            {
                return Json(new
                {
                    success = false,
                    matched = false,
                    message =
                        "Matched student was not found."
                });
            }


            // ============================================
            // CHECK DUPLICATE ATTENDANCE
            // ============================================

            var today =
                DateTime.Today;


            var alreadyMarked =
                await _context.Attendances
                    .AnyAsync(a =>
                        a.StudentId ==
                            student.Id &&
                        a.AttendanceDate.Date ==
                            today);


            if (alreadyMarked)
            {
                return Json(new
                {
                    success = false,
                    matched = true,
                    alreadyMarked = true,
                    studentName =
                        student.Name,
                    enrollmentNo =
                        student.EnrollmentNo,
                    message =
                        "Attendance is already marked for today."
                });
            }


            // ============================================
            // CREATE ATTENDANCE
            // ============================================

            var now =
                DateTime.Now;


            var attendance =
                new Attendance
                {
                    StudentId =
                        student.Id,

                    AttendanceDate =
                        today,

                    AttendanceTime =
                        now,

                    Status =
                        "Present",

                    Method =
                        "Face"
                };


            _context.Attendances.Add(
                attendance);


            await _context.SaveChangesAsync();


            // ============================================
            // SUCCESS
            // ============================================

            return Json(new
            {
                success = true,
                matched = true,

                studentName =
                    student.Name,

                enrollmentNo =
                    student.EnrollmentNo,

                date =
                    today.ToString(
                        "dd-MM-yyyy"),

                time =
                    now.ToString(
                        "hh:mm:ss tt"),

                status =
                    "Present",

                method =
                    "Face",

                message =
                    "Attendance marked successfully."
            });
        }


        // ============================================
        // DETAILS
        // ============================================

        public async Task<IActionResult> Details(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var attendance =
                await _context.Attendances
                    .Include(a => a.Student)
                    .FirstOrDefaultAsync(
                        a => a.Id == id);


            if (attendance == null)
            {
                return NotFound();
            }


            return View(attendance);
        }


        // ============================================
        // DELETE GET
        // ============================================

        public async Task<IActionResult> Delete(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var attendance =
                await _context.Attendances
                    .Include(a => a.Student)
                    .FirstOrDefaultAsync(
                        a => a.Id == id);


            if (attendance == null)
            {
                return NotFound();
            }


            return View(attendance);
        }


        // ============================================
        // DELETE POST
        // ============================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var attendance =
                await _context.Attendances
                    .FindAsync(id);


            if (attendance != null)
            {
                _context.Attendances.Remove(
                    attendance);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Attendance record deleted successfully.";
            }


            return RedirectToAction(
                nameof(Index));
        }
    }
}