using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using openiddict_password_degradedmode;
using openiddict_password_degradedmode.Contexts;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static openiddict_password_degradedmode.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    //options.UseSqlServer("Data Source=localhost;Initial Catalog=openiddict_password_degradedmode;User Id=myuser;Password=mypassword;TrustServerCertificate=True");
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=aspnet-BlazorApp1-123e7398-fb9e-4421-960b-b2d66a607b4d;Trusted_Connection=True;MultipleActiveResultSets=true");
});

// Register the Identity services.
builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();

        options.AddEventHandler(ProcessRequestContextHandler.Descriptor);

        options.AddEventHandler(Handlers.ProcessChallengeContextHandler.Descriptor);
    });

builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        // Enable the token endpoint.
        options.SetTokenEndpointUris("connect/token");

        // Enable the password flow.
        options.AllowPasswordFlow();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement(); // disable https requirement

        options.EnableDegradedMode();

        options.SetAccessTokenLifetime(TimeSpan.FromSeconds(300));

        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ValidateTokenRequestContext>(builder =>
            builder.UseInlineHandler(context =>
            {
                if (context.ClientId != "someclient" || context.ClientSecret != "somesecret")
                {
                    context.Logger.LogError($"Invalid client {context.ClientId}");
                    context.Reject(error: Errors.InvalidClient, description: "Client id/secret did not match.");
                }

                return default;
            }
        ));

        options.AddEventHandler(Handlers.ApplyTokenResponseContextHandler.Descriptor);
    })
    .AddCore();


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
})
    .AddBearerToken();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();
app.UseAuthorization();

app.MapControllers();

app.Run();
