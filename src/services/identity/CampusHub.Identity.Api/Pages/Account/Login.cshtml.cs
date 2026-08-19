using System.ComponentModel.DataAnnotations;
using CampusHub.Identity.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Identity.Api.Pages.Account;

/// <summary>SDD CH-S17 — specs/017-auth-session. Email/password login UI at Identity /Account/Login.</summary>
[AllowAnonymous]
public sealed class LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public sealed class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            logger.LogInformation("User {Email} signed in", Input.Email);
            return LocalRedirect(ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }
}
