using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using DuendeAuth.Common.Constants;
using DuendeAuth.Data;
using Microsoft.AspNetCore.Identity;

namespace DuendeAuth;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var config = sp.GetRequiredService<IConfiguration>();

        await sp.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await sp.GetRequiredService<PersistedGrantDbContext>().Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var configDb = sp.GetRequiredService<ConfigurationDbContext>();
        await configDb.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        if (!configDb.Clients.Any())
        {
            foreach (var client in Config.GetClients(config))
                configDb.Clients.Add(client.ToEntity());
            await configDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var existingIdentityResourceNames = configDb.IdentityResources.Select(r => r.Name).ToHashSet();
        foreach (var resource in Config.IdentityResources.Where(r => !existingIdentityResourceNames.Contains(r.Name)))
            configDb.IdentityResources.Add(resource.ToEntity());
        if (configDb.ChangeTracker.HasChanges())
            await configDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var existingScopeNames = configDb.ApiScopes.Select(s => s.Name).ToHashSet();
        foreach (var apiScope in Config.ApiScopes.Where(s => !existingScopeNames.Contains(s.Name)))
            configDb.ApiScopes.Add(apiScope.ToEntity());
        if (configDb.ChangeTracker.HasChanges())
            await configDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var existingResourceNames = configDb.ApiResources.Select(r => r.Name).ToHashSet();
        foreach (var resource in Config.ApiResources.Where(r => !existingResourceNames.Contains(r.Name)))
            configDb.ApiResources.Add(resource.ToEntity());
        if (configDb.ChangeTracker.HasChanges())
            await configDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var adminPassword = config[ConfigKeys.AdminPassword]
            ?? throw new InvalidOperationException($"{ConfigKeys.AdminPassword} is not configured.");

        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        if (await userManager.FindByNameAsync(SeedDefaults.AdminUserName).ConfigureAwait(false) is null)
        {
            var admin = new IdentityUser
            {
                UserName = SeedDefaults.AdminUserName,
                Email = SeedDefaults.AdminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, adminPassword).ConfigureAwait(false);
        }
    }
}
