using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DuendeAuth.Admin.Models;

/// <summary>Request body for creating a new user.</summary>
public record CreateUserRequest(
    [property: Required]
    [property: StringLength(256, MinimumLength = 1)]
    [property: Description("The desired login name. Must be unique.")] string UserName,
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(256)]
    [property: Description("The user's email address.")] string Email,
    [property: Required]
    [property: Description("The initial password. Must meet the configured password policy.")] string Password);
