using DuendeAuth;
using DuendeAuth.Admin;
using DuendeAuth.Common.Constants;
using DuendeAuth.Data;
using DuendeAuth.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseConfiguredProvider(builder.Configuration, ConnectionStringNames.Identity));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddIdentityServer()
    .AddAspNetIdentity<IdentityUser>()
    .AddDeveloperSigningCredential()
    .AddConfigurationStore(options =>
        options.ConfigureDbContext = b =>
            b.UseConfiguredProvider(builder.Configuration, ConnectionStringNames.Config, ConnectionStringNames.ConfigMigrationsAssembly))
    .AddOperationalStore(options =>
        options.ConfigureDbContext = b =>
            b.UseConfiguredProvider(builder.Configuration, ConnectionStringNames.Grants, ConnectionStringNames.GrantsMigrationsAssembly));

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration[ConfigKeys.Authority];
        options.TokenValidationParameters.ValidAudiences = [Scopes.DuendeManage, Scopes.DuendeRead];
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.Admin, policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireClaim("scope", Scopes.DuendeManage);
    });
    options.AddPolicy(PolicyNames.AdminRead, policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireClaim("scope", Scopes.DuendeManage, Scopes.DuendeRead);
    });
});

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        var authority = builder.Configuration[ConfigKeys.Authority];
        document.Info = new OpenApiInfo { Title = "DuendeAuth Admin", Version = "v1" };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            [PolicyNames.OAuthSecurityScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri($"{authority}/connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            [Scopes.DuendeManage] = "Full management access (users, claims, clients)"
                        }
                    }
                }
            }
        };
        return Task.CompletedTask;
    }));

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(title: "An unexpected error occurred.", statusCode: 500)
            .ExecuteAsync(context);
    }));

await SeedData.InitializeAsync(app.Services, default);

app.UseIdentityServer();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(options =>
    options.WithTitle("DuendeAuth Admin")
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
           .AddPreferredSecuritySchemes(PolicyNames.OAuthSecurityScheme));

app.MapAdminEndpoints();

app.Run();
