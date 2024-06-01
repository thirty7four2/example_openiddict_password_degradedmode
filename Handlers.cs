using OpenIddict.Validation;
using static OpenIddict.Validation.OpenIddictValidationEvents;

namespace openiddict_password_degradedmode
{
    public static class Handlers
    {
        public class ProcessChallengeContextHandler : IOpenIddictValidationHandler<ProcessChallengeContext>
        {
            public ProcessChallengeContextHandler(IHttpContextAccessor accessor) => _accessor = accessor;

            private readonly IHttpContextAccessor _accessor;

            public static OpenIddictValidationHandlerDescriptor Descriptor { get; }
                = OpenIddictValidationHandlerDescriptor.CreateBuilder<ProcessChallengeContext>()
                    .UseSingletonHandler<ProcessChallengeContextHandler>()
                    .SetType(OpenIddictValidationHandlerType.Custom)
                    .Build();

            ValueTask IOpenIddictValidationHandler<ProcessChallengeContext>.HandleAsync(ProcessChallengeContext context)
            {
                var authorization = _accessor.HttpContext.Request.Headers["Authorization"];

                var split = authorization.ToString().Split(' ');

                var token = split.ElementAtOrDefault(1);

                context.Logger.LogError($"Bad token request: {token} at {context.RequestUri}");

                return default;
            }
        }
    }
}
