using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Student_Attendence_System.Data;
using Student_Attendence_System.Models;

namespace Student_Attendence_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        // ==========================================
        // LOGIN
        // ==========================================

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
    string email,
    string password,
    string role,
    bool rememberMe = false,
    string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError(
                    "",
                    "Role, email and password are required.");

                return View();
            }

            if (role != "Student" &&
                role != "Faculty" &&
                role != "Admin")
            {
                ModelState.AddModelError(
                    "",
                    "Invalid login role.");

                return View();
            }

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View();
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    password,
                    rememberMe,
                    lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(
                        "",
                        "Account temporarily locked. Try again later.");
                }
                else
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid email or password.");
                }

                return View();
            }

            // Check selected role
            var hasRole =
                await _userManager.IsInRoleAsync(
                    user,
                    role);

            if (!hasRole)
            {
                await _signInManager.SignOutAsync();

                ModelState.AddModelError(
                    "",
                    $"This account does not have the {role} role.");

                return View();
            }

            if (role == "Faculty")
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            if (role == "Student")
            {
                return RedirectToAction(
                    "Index",
                    "StudentPortal");
            }

            await _signInManager.SignOutAsync();

            ModelState.AddModelError(
                "",
                "Your account does not have a valid role.");

            return View();
        }

        // ==========================================
        // STUDENT REGISTRATION
        // ==========================================

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check enrollment number
            var enrollmentExists =
                _context.Students.Any(
                    s => s.EnrollmentNo == model.EnrollmentNo);

            if (enrollmentExists)
            {
                ModelState.AddModelError(
                    "EnrollmentNo",
                    "This enrollment number is already registered.");

                return View(model);
            }

            // Check email
            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email address is already registered.");

                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // ------------------------------------------
                // Create Student
                // ------------------------------------------

                var student = new Student
                {
                    EnrollmentNo = model.EnrollmentNo,
                    Name = model.Name,
                    Email = model.Email,
                    ClassName = model.ClassName,
                    CreatedAt = DateTime.Now
                };

                _context.Students.Add(student);

                await _context.SaveChangesAsync();


                // ------------------------------------------
                // Create Identity User
                // ------------------------------------------

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,

                    // Connect Identity account
                    // to Student record
                    StudentId = student.Id,

                    EmailConfirmed = true
                };

                var createResult =
                    await _userManager.CreateAsync(
                        user,
                        model.Password);

                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    await transaction.RollbackAsync();

                    return View(model);
                }


                // ------------------------------------------
                // Assign STUDENT role
                // ------------------------------------------

                var roleResult =
                    await _userManager.AddToRoleAsync(
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

                    await transaction.RollbackAsync();

                    return View(model);
                }


                // Everything succeeded
                await transaction.CommitAsync();


                // Automatically login
                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false);


                return RedirectToAction(
                    "Index",
                    "StudentPortal");
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Registration failed. Please try again.");

                return View(model);
            }
        }


        // ==========================================
        // LOGOUT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                nameof(Login));
        }


        // ==========================================
        // ACCESS DENIED
        // ==========================================

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}