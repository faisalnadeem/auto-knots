using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutoKnots.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (!Url.IsLocalUrl(returnUrl)) returnUrl = Url.Content("~/");
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return Redirect($"/auth-login-cover.html?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Redirect($"/auth-login-cover.html?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
            var result = await _signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return Redirect($"/auth-login-cover.html?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Register(string email, string password, string? confirmPassword = null, string? fullName = null, string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && !Url.IsLocalUrl(returnUrl)) returnUrl = null;
            var returnQuery = string.IsNullOrEmpty(returnUrl) ? string.Empty : $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return Redirect($"/auth-register-cover.html?error=1{returnQuery}");
            }
            if (password != confirmPassword)
            {
                return Redirect($"/auth-register-cover.html?error=2{returnQuery}");
            }
            var user = new IdentityUser
            {
                UserName = email,
                Email = email
            };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return Redirect($"/auth-register-cover.html?error=1{returnQuery}");
            }
            return string.IsNullOrEmpty(returnUrl)
                ? Redirect("/auth-login-cover.html")
                : Redirect($"/auth-login-cover.html?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            return string.IsNullOrEmpty(returnUrl)
                ? Redirect("/auth-login-cover.html")
                : LocalRedirect(returnUrl);
        }
    }
}
