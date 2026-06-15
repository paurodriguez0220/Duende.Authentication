using Duende.IdentityServer.Models;
using DuendeAuth.Common.Constants;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope(Scopes.ScalarApi, "Scalar API"),
        new ApiScope(Scopes.DuendeManage, "Full management access (users, claims, clients)"),
        new ApiScope(Scopes.DuendeRead, "Read-only access (users, claims, clients)")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource(Scopes.ScalarApi, "Scalar API") { Scopes = { Scopes.ScalarApi } },
        new ApiResource(Scopes.DuendeManage, "Full management access") { Scopes = { Scopes.DuendeManage } },
        new ApiResource(Scopes.DuendeRead, "Read-only access") { Scopes = { Scopes.DuendeRead } }
    ];

    public static IEnumerable<Client> GetClients(IConfiguration configuration) =>
    [
        new Client
        {
            ClientId = ClientIds.ScalarClient,
            ClientSecrets =
            {
                new Secret(
                    (configuration[ConfigKeys.ScalarClientSecret]
                        ?? throw new InvalidOperationException($"{ConfigKeys.ScalarClientSecret} is not configured."))
                    .Sha256())
            },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { Scopes.ScalarApi },
            AllowedCorsOrigins = { CorsOrigins.ScalarApiLocal }
        },
        new Client
        {
            ClientId = ClientIds.AdminClient,
            ClientSecrets =
            {
                new Secret(
                    (configuration[ConfigKeys.AdminClientSecret]
                        ?? throw new InvalidOperationException($"{ConfigKeys.AdminClientSecret} is not configured."))
                    .Sha256())
            },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { Scopes.DuendeManage }
        },
        new Client
        {
            ClientId = ClientIds.WatcherClient,
            ClientSecrets =
            {
                new Secret(
                    (configuration[ConfigKeys.WatcherClientSecret]
                        ?? throw new InvalidOperationException($"{ConfigKeys.WatcherClientSecret} is not configured."))
                    .Sha256())
            },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { Scopes.DuendeRead }
        }
    ];
}
