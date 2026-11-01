using Microsoft.AspNetCore.Identity;
using Student_Attendence_System.Models;

namespace Student_Attendence_System.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            IServiceProvider services)
        {
            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                services.GetRequiredService<UserManager<ApplicationUser>>();

            // -------------------------
            // Create roles
            // -------------------------

            string[] roles =
            {
                "Faculty",
                "Student"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result =
                        await roleManager.CreateAsync(
                            new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        throw new Exception(
                            "Unable to create role: " + role);
                    }
                }
            }

            const string facultyEmail =
                "Faculty1@gmail.com";

            const string facultyPassword =
                "Faculty1@123";

            var faculty =
                await userManager.FindByEmailAsync(
                    facultyEmail);

            if (faculty == null)
            {
                faculty = new ApplicationUser
                {
                    UserName = facultyEmail,
                    Email = facultyEmail,
                    EmailConfirmed = true
                };

                var result =
                    await userManager.CreateAsync(
                        faculty,
                        facultyPassword);

                if (!result.Succeeded)
                {
                    throw new Exception(
                        "Unable to create Faculty account: " +
                        string.Join(
                            ", ",
                            result.Errors.Select(
                                e => e.Description)));
                }
            }

            // Ensure Faculty role
            if (!await userManager.IsInRoleAsync(
                    faculty,
                    "Faculty"))
            {
                var result =
                    await userManager.AddToRoleAsync(
                        faculty,
                        "Faculty");

                if (!result.Succeeded)
                {
                    throw new Exception(
                        "Unable to assign Faculty role.");
                }
            }
        }
    }
}