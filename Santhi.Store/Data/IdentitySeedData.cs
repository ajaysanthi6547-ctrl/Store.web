using Microsoft.AspNetCore.Identity;

namespace Santhi.Store.Data
{
    public static class IdentitySeedData
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var configuration = scope.ServiceProvider
                .GetRequiredService<IConfiguration>();

            string email = configuration["AdminAccount:Email"]
                ?? throw new Exception("Admin email is missing.");

            string password = configuration["AdminAccount:Password"]
                ?? throw new Exception("Admin password is missing.");

            // Create Admin role
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            }

            // Create Customer role
            if (!await roleManager.RoleExistsAsync("Customer"))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole("Customer"));

                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(
                        ", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Find or create Admin user
            var admin = await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, password);

                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(
                        ", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Assign Admin role
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}