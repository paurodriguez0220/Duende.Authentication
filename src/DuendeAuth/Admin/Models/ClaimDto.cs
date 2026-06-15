using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DuendeAuth.Admin.Models;

/// <summary>A user claim.</summary>
public record ClaimDto(
    [property: Required]
    [property: StringLength(256, MinimumLength = 1)]
    [property: Description("The claim type, e.g. role.")] string Type,
    [property: Required]
    [property: StringLength(256)]
    [property: Description("The claim value.")] string Value);
