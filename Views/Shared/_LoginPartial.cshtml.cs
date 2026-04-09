using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyIQ.Models;

namespace SkyIQ.Views.Shared
{
    public class _LoginPartial:PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<_LoginPartial> _logger;

        public _LoginPartial(SignInManager<ApplicationUser> signInManager, ILogger<_LoginPartial> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
