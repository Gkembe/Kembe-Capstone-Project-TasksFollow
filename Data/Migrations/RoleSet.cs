using Microsoft.AspNetCore.Identity;
using TasksFollow.Models;

namespace TasksFollow.Data.Migrations
{
    public static class RoleSet
    {
        public static async Task SetRolesAndAdmin(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            String[] roles = { "Manager", "Admin", "Employee" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }

            }


            //SEED First admin for SAAS

            string adminEmail = "admin@gmail.com";

            ApplicationUser? admin =
                await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Gedeon",
                    MiddleName = "N",
                    LastName = "Kembe",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    admin,
                    "Admin123!");

                if (!result.Succeeded)
                {
                    throw new Exception(
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Make sure the existing/new user has Admin role
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
