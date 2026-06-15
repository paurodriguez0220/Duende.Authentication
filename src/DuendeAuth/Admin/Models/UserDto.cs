using System.ComponentModel;

namespace DuendeAuth.Admin.Models;

/// <summary>User details.</summary>
public record UserDto(
    [property: Description("The user's unique identifier.")] string Id,
    [property: Description("The user's login name.")] string UserName,
    [property: Description("The user's email address.")] string? Email,
    [property: Description("Whether the email address has been confirmed.")] bool EmailConfirmed);
