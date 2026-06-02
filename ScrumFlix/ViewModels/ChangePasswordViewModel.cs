/*
 * File:        /ScrumFlix/ViewModels/ChangePasswordViewModel.cs
 * Namespace:   ScrumFlix.ViewModels
 * Purpose:     View model for the Account/ChangePassword form.
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */


namespace ScrumFlix.ViewModels;

/// <summary>
/// Binds the ChangePassword form POST body.
/// </summary>
public sealed class ChangePasswordViewModel
{
    /// <summary>The user's current password, re-verified before allowing the change.</summary>
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>The new password the user wants to set.</summary>
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(128, MinimumLength = 8,
        ErrorMessage = "Password must be between 8 and 128 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Confirmation field — must match NewPassword.</summary>
    [Required(ErrorMessage = "Please confirm your new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// When true the form is being shown because the system is forcing a change
    /// (MustChangePassword = true). The view uses this to display a notice
    /// explaining why the change is required.
    /// </summary>
    public bool IsForced { get; set; }
}
