using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DuendeAuth.Admin.Models;

/// <summary>Request body for registering a new OAuth2 client.</summary>
public record CreateClientRequest(
    [property: Required]
    [property: StringLength(200, MinimumLength = 1)]
    [property: Description("The unique client identifier.")] string ClientId,
    [property: Required]
    [property: StringLength(500, MinimumLength = 1)]
    [property: Description("The client secret (plain text — hashed before storage).")] string Secret,
    [property: Required]
    [property: MinLength(1)]
    [property: Description("The scopes this client is permitted to request.")] List<string> AllowedScopes,
    [property: Description("Allowed CORS origins for browser-based clients. Optional.")] List<string>? AllowedCorsOrigins,
    [property: Description("Grant type: 'client_credentials' (default, machine-to-machine) or 'authorization_code' (user login).")] string? GrantType,
    [property: Description("Redirect URIs after login. Required when GrantType is 'authorization_code'.")] List<string>? RedirectUris,
    [property: Description("Redirect URIs after logout. Optional, used with 'authorization_code'.")] List<string>? PostLogoutRedirectUris);
