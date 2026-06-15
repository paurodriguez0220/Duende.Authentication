using System.ComponentModel;

namespace DuendeAuth.Admin.Models;

/// <summary>Summary of a registered OAuth2 client.</summary>
public record ClientSummaryDto(
    [property: Description("The client identifier used in token requests.")] string ClientId,
    [property: Description("The scopes this client is permitted to request.")] IReadOnlyList<string> AllowedScopes,
    [property: Description("The OAuth2 grant types this client may use.")] IReadOnlyList<string> AllowedGrantTypes);
