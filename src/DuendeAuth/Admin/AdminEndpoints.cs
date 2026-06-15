using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DuendeAuth.Admin.Models;
using DuendeAuth.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DuendeAuth.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .RequireAuthorization(PolicyNames.Admin)
            .WithTags("Admin")
            .ProducesProblem(401)
            .ProducesProblem(403);

        group.MapGet("/users", GetUsersAsync)
            .WithSummary("List all users")
            .Produces<PagedResult<UserDto>>();

        group.MapPost("/users", CreateUserAsync)
            .WithSummary("Create a user")
            .Produces<UserDto>(201)
            .ProducesProblem(400);

        group.MapDelete("/users/{id}", DeleteUserAsync)
            .WithSummary("Delete a user")
            .Produces(204)
            .ProducesProblem(404);

        group.MapGet("/users/{id}/claims", GetUserClaimsAsync)
            .WithSummary("List a user's custom claims")
            .Produces<List<ClaimDto>>()
            .ProducesProblem(404);

        group.MapPost("/users/{id}/claims", AddUserClaimAsync)
            .WithSummary("Add a claim to a user")
            .Produces<ClaimDto>(201)
            .ProducesProblem(400)
            .ProducesProblem(404);

        group.MapDelete("/users/{id}/claims/{type}", RemoveUserClaimAsync)
            .WithSummary("Remove a claim from a user")
            .Produces(204)
            .ProducesProblem(404);

        group.MapGet("/clients", GetClients)
            .WithSummary("List registered OAuth2 clients")
            .Produces<List<ClientSummaryDto>>();

        return app;
    }

    private static async Task<IResult> GetUsersAsync(
        UserManager<IdentityUser> userManager,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        const int maxPageSize = 100;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, maxPageSize);

        var totalCount = await userManager.Users.CountAsync(ct).ConfigureAwait(false);
        var users = await userManager.Users
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto(u.Id, u.UserName!, u.Email, u.EmailConfirmed))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return Results.Ok(new PagedResult<UserDto>(users, new PaginationMeta(page, pageSize, totalCount, totalPages)));
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
            return Results.ValidationProblem(
                validationResults
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid value.").ToArray()));

        var user = new IdentityUser { UserName = request.UserName, Email = request.Email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);

        if (!result.Succeeded)
            return Results.ValidationProblem(
                result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        return Results.Created($"/api/v1/admin/users/{user.Id}",
            new UserDto(user.Id, user.UserName!, user.Email, user.EmailConfirmed));
    }

    private static async Task<IResult> DeleteUserAsync(
        string id,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id).ConfigureAwait(false);
        if (user is null) return Results.NotFound();

        await userManager.DeleteAsync(user).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> GetUserClaimsAsync(
        string id,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id).ConfigureAwait(false);
        if (user is null) return Results.NotFound();

        var claims = await userManager.GetClaimsAsync(user).ConfigureAwait(false);
        return Results.Ok(claims.Select(c => new ClaimDto(c.Type, c.Value)));
    }

    private static async Task<IResult> AddUserClaimAsync(
        string id,
        ClaimDto request,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
            return Results.ValidationProblem(
                validationResults
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid value.").ToArray()));

        var user = await userManager.FindByIdAsync(id).ConfigureAwait(false);
        if (user is null) return Results.NotFound();

        var result = await userManager.AddClaimAsync(user, new Claim(request.Type, request.Value)).ConfigureAwait(false);

        if (!result.Succeeded)
            return Results.ValidationProblem(
                result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        return Results.Created($"/api/v1/admin/users/{id}/claims", request);
    }

    private static async Task<IResult> RemoveUserClaimAsync(
        string id,
        string type,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id).ConfigureAwait(false);
        if (user is null) return Results.NotFound();

        var claims = await userManager.GetClaimsAsync(user).ConfigureAwait(false);
        var claim = claims.FirstOrDefault(c => c.Type == type);
        if (claim is null) return Results.NotFound();

        await userManager.RemoveClaimAsync(user, claim).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static IResult GetClients(IConfiguration configuration) =>
        Results.Ok(Config.GetClients(configuration)
            .Select(c => new ClientSummaryDto(
                c.ClientId,
                c.AllowedScopes.ToList(),
                c.AllowedGrantTypes.ToList())));
}
