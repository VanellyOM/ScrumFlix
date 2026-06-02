/*
 * File:        /ScrumFlix/ViewModels/LoginViewModel.cs
 * Namespace:   ScrumFlix.ViewModels
 * Purpose:     View model for the Account/Login form.
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */


namespace ScrumFlix.ViewModels;

/// <summary>
/// Binds the Login form POST body.
/// </summary>
public sealed class LoginViewModel
{
    /// <summary>The submitted username.</summary>
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>The submitted password (plaintext — never stored as-is).</summary>
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The URL to redirect to after a successful login.
    /// Populated by the Login GET action from the returnUrl route value.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
