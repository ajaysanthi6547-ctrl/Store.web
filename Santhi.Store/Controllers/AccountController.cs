
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Santhi.Store.Models;

namespace Santhi.Store.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // Login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Customer and Admin login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginLimit")]
        public async Task<IActionResult> Login(
            string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                isPersistent: false,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                var loggedInUser =
                    await _userManager.FindByEmailAsync(email);

                if (loggedInUser != null &&
                    await _userManager.IsInRoleAsync(
                        loggedInUser, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Index", "Home");
            }

            // Generic login error message
            ViewBag.Error = "Invalid email or password.";

            return View();
        }

        // Customer registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Create customer account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = false
            };

            // Identity automatically hashes the password
            var result = await _userManager.CreateAsync(
                user, model.Password);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(
                    user, "Customer");

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "", error.Description);
                    }

                    // Remove partially created account
                    await _userManager.DeleteAsync(user);

                    return View(model);
                }

                await _signInManager.SignInAsync(
                    user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "", error.Description);
            }

            return View(model);
        }

        // Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login");
        }

    }
}
