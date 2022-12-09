namespace Irrbloss.Extensions;

using Irrbloss.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class ManagedResponseExceptionMiddlewareExtensions
{
    public static IServiceCollection AddManagedResponseException(this IServiceCollection services)
    {
        services.TryAddSingleton<IActionResultExecutor<ObjectResult>, ObjectResultExecutor>();
        return services;
    }

    public static IApplicationBuilder UseManagedResponseException(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ManagedResponseExceptionMiddleware>();
    }
}
