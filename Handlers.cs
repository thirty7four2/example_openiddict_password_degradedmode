using Microsoft.EntityFrameworkCore;
using OpenIddict.Server;
using OpenIddict.Validation;
using openiddict_password_degradedmode.Contexts;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace openiddict_password_degradedmode
{
    public static class Handlers
    {
        public class ProcessRequestContextHandler : IOpenIddictValidationHandler<OpenIddictValidationEvents.ProcessRequestContext>
        {
            public ProcessRequestContextHandler(IHttpContextAccessor accessor, ApplicationDbContext applicationDbContext)
            {
                _accessor = accessor ?? throw new ArgumentNullException();
                _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException();
            }

            private readonly IHttpContextAccessor _accessor;
            private readonly ApplicationDbContext _applicationDbContext;

            public static OpenIddictValidationHandlerDescriptor Descriptor { get; }
                = OpenIddictValidationHandlerDescriptor.CreateBuilder<OpenIddictValidationEvents.ProcessRequestContext>()
                    .UseScopedHandler<ProcessRequestContextHandler>()
                    .SetType(OpenIddictValidationHandlerType.Custom)
                    .Build();

            async ValueTask IOpenIddictValidationHandler<OpenIddictValidationEvents.ProcessRequestContext>.HandleAsync(OpenIddictValidationEvents.ProcessRequestContext context)
            {
                if (context.RequestUri.AbsolutePath == "/connect/token")
                {
                    context.Logger.LogInformation($"Got request at {context.RequestUri}");
                }
                else
                {
                    // check if token is still marked valid

                    var authorization = _accessor.HttpContext.Request.Headers["Authorization"];

                    var split = authorization.ToString().Split(' ');

                    var tokenId = split.ElementAtOrDefault(1);

                    var token = await _applicationDbContext.AccessTokens.FirstOrDefaultAsync(a => a.Id == tokenId);

                    if (token == null || !token.IsValid)
                    {
                        context.Logger.LogError($"Invalid token");
                        context.Reject(error: Errors.InvalidToken, description: "Invalid token.");
                    }
                }

                return;
            }
        }

        public class ProcessChallengeContextHandler : IOpenIddictValidationHandler<OpenIddictValidationEvents.ProcessChallengeContext>
        {
            public ProcessChallengeContextHandler(IHttpContextAccessor accessor) => _accessor = accessor;

            private readonly IHttpContextAccessor _accessor;

            public static OpenIddictValidationHandlerDescriptor Descriptor { get; }
                = OpenIddictValidationHandlerDescriptor.CreateBuilder<OpenIddictValidationEvents.ProcessChallengeContext>()
                    .UseSingletonHandler<ProcessChallengeContextHandler>()
                    .SetType(OpenIddictValidationHandlerType.Custom)
                    .Build();

            ValueTask IOpenIddictValidationHandler<OpenIddictValidationEvents.ProcessChallengeContext>.HandleAsync(OpenIddictValidationEvents.ProcessChallengeContext context)
            {
                var authorization = _accessor.HttpContext.Request.Headers["Authorization"];

                var split = authorization.ToString().Split(' ');

                var token = split.ElementAtOrDefault(1);

                context.Logger.LogError($"Bad token request: {token} at {context.RequestUri}");

                return default;
            }
        }

        public class ApplyTokenResponseContextHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
        {
            public ApplyTokenResponseContextHandler(IHttpContextAccessor accessor, ApplicationDbContext applicationDbContext)
            {
                _accessor = accessor ?? throw new ArgumentNullException();
                _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException();
            }

            private readonly IHttpContextAccessor _accessor;
            private readonly ApplicationDbContext _applicationDbContext;

            public static OpenIddictServerHandlerDescriptor Descriptor { get; }
                = OpenIddictServerHandlerDescriptor.CreateBuilder<ApplyTokenResponseContext>()
                    .UseScopedHandler<ApplyTokenResponseContextHandler>()
                    .SetType(OpenIddictServerHandlerType.Custom)
                    .Build();

            async ValueTask IOpenIddictServerHandler<ApplyTokenResponseContext>.HandleAsync(ApplyTokenResponseContext context)
            {
                if (string.IsNullOrWhiteSpace(context.Error))
                {
                    // store the token
                    var token = new AccessToken
                    {
                        Id = context.Response.AccessToken,
                        CreatedUtc = DateTime.UtcNow,
                        IsValid = true,
                        ClientId = context.Request.ClientId
                    };

                    _applicationDbContext.AccessTokens.Add(token);
                    await _applicationDbContext.SaveChangesAsync();

                    // can add parameter to response here
                    context.Response.AddParameter("some_int", new OpenIddict.Abstractions.OpenIddictParameter(1));
                    context.Response.AddParameter("some_string", new OpenIddict.Abstractions.OpenIddictParameter("foo"));
                }

                return;
            }
        }
    }
}
