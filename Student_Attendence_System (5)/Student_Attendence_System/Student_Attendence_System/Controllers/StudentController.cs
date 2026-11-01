using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendence_System;
using Student_Attendence_System.Data;
using Student_Attendence_System.Models;
using YourProjectName.Models;

namespace Student_Attendence_System.Controllers
{
    [Authorize(Roles = "Faculty")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FaceRecognitionClient _faceRecognitionClient;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(
            ApplicationDbContext context,
            FaceRecognitionClient faceRecognitionClient,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _faceRecognitionClient = faceRecognitionClient;
            _userManager = userManager;
        }


        // ============================================
        // STUDENT LIST
        // ============================================

        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(students);
        }


        // ============================================
        // DETAILS
        // ============================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }


        // ============================================
        // CREATE - GET
        // ============================================

        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStudentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            bool enrollmentExists = await _context.Students
                .AnyAsync(s => s.EnrollmentNo == model.EnrollmentNo);

            if (enrollmentExists)
            {
                ModelState.AddModelError(
                    "EnrollmentNo",
                    "This enrollment number already exists.");

                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                bool emailExists = await _context.Students
                    .AnyAsync(s => s.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email address is already registered.");

                    return View(model);
                }
            }
            var student = new Student
            {
                EnrollmentNo = model.EnrollmentNo,
                Name = model.Name,
                Email = model.Email,
                ClassName = model.ClassName
            };

            _context.Students.Add(student);

            await _context.SaveChangesAsync();
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    StudentId = student.Id
                };

                var result = await _userManager.CreateAsync(
    user,
    model.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(
                            "Password",
                            error.Description);
                    }

                    // Remove student if account creation failed
                    _context.Students.Remove(student);
                    await _context.SaveChangesAsync();

                    return View(model);
                }


                // ============================================
                // ASSIGN STUDENT ROLE
                // ============================================

                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    "Student");

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    // Remove Identity user
                    await _userManager.DeleteAsync(user);

                    // Remove student
                    _context.Students.Remove(student);
                    await _context.SaveChangesAsync();

                    return View(model);
            }
            }


            TempData["Success"] =
                "Student and student login account created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ============================================
        // EDIT - GET
        // ============================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student =
                await _context.Students
                    .FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }


        // ============================================
        // EDIT - POST
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Student updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(
                    nameof(Index));
            }

            return View(student);
        }


        // ============================================
        // DELETE - GET
        // ============================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student =
                await _context.Students
                    .FirstOrDefaultAsync(
                        s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }


        // ============================================
        // DELETE - POST
        // ============================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var student =
                await _context.Students
                    .FindAsync(id);

            if (student != null)
            {
                // ========================================
                // DELETE LOCAL FACE EMBEDDING FIRST
                // ========================================

                try
                {
                    await _faceRecognitionClient
                        .DeleteAsync(student.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "FACE DELETE ERROR:");

                    Console.WriteLine(
                        ex.ToString());

                    TempData["Error"] =
                        "Student was not deleted because " +
                        "the face data could not be removed.";

                    return RedirectToAction(
                        nameof(Index));
                }


                // ========================================
                // DELETE STUDENT FROM DATABASE
                // ========================================

                _context.Students.Remove(student);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Student and face data deleted successfully.";
            }

            return RedirectToAction(
                nameof(Index));
        }


        // ============================================
        // REGISTER FACE - GET
        // ============================================

        public async Task<IActionResult> RegisterFace(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student =
                await _context.Students
                    .FirstOrDefaultAsync(
                        s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }


        // ============================================
        // SAVE FACE - POST
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFace(
            int studentId,
            IFormFile faceImage)
        {
            // ============================================
            // CHECK IMAGE
            // ============================================

            if (faceImage == null ||
                faceImage.Length == 0)
            {
                TempData["Error"] =
                    "No face image was received.";

                return RedirectToAction(
                    nameof(RegisterFace),
                    new { id = studentId });
            }


            // ============================================
            // FIND STUDENT
            // ============================================

            var student =
                await _context.Students
                    .FindAsync(studentId);

            if (student == null)
            {
                return NotFound();
            }


            // ============================================
            // READ IMAGE INTO MEMORY
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


            if (imageBytes.Length == 0)
            {
                TempData["Error"] =
                    "The captured image is empty.";

                return RedirectToAction(
                    nameof(RegisterFace),
                    new { id = studentId });
            }


            // ============================================
            // CHECK WHETHER FACE ALREADY BELONGS
            // TO ANOTHER STUDENT
            // ============================================

            try
            {
                int? existingStudentId =
                    await _faceRecognitionClient
                        .FindMatchingStudentAsync(imageBytes);


                // ============================================
                // FACE ALREADY REGISTERED TO ANOTHER STUDENT
                // ============================================

                if (existingStudentId.HasValue &&
                    existingStudentId.Value != student.Id)
                {
                    var existingStudent =
                        await _context.Students
                            .FindAsync(existingStudentId.Value);

                    string existingStudentName =
                        existingStudent?.Name
                        ?? "another student";

                    TempData["Error"] =
                        $"This face is already registered to " +
                        $"{existingStudentName}. " +
                        "The same face cannot be registered " +
                        "to another student.";

                    return RedirectToAction(
                        nameof(RegisterFace),
                        new { id = studentId });
                }


                // ============================================
                // REGISTER FACE
                // ============================================

                bool registered =
                    await _faceRecognitionClient
                        .RegisterAsync(
                            student.Id,
                            imageBytes);


                if (!registered)
                {
                    TempData["Error"] =
                        "Face registration failed. " +
                        "The face engine did not register the face.";

                    return RedirectToAction(
                        nameof(RegisterFace),
                        new { id = studentId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "FACE REGISTRATION ERROR:");

                Console.WriteLine(
                    ex.ToString());

                TempData["Error"] =
                    "Face engine error: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(RegisterFace),
                    new { id = studentId });
            }


            // ============================================
            // DO NOT STORE ORIGINAL PHOTO
            // ============================================

            student.FaceImagePath = null;

            student.FaceData =
                "Local face embedding registered";


            await _context.SaveChangesAsync();


            // ============================================
            // SUCCESS
            // ============================================

            TempData["Success"] =
                "Face registered successfully. " +
                "The original photo was not stored.";

            return RedirectToAction(
                nameof(Index));
        }


        // ============================================
        // STUDENT EXISTS
        // ============================================

        private bool StudentExists(int id)
        {
            return _context.Students
                .Any(s => s.Id == id);
        }
    }
}