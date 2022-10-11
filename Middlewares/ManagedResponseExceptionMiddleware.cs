namespace Irrbloss.Middlewares;

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Irrbloss.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;

public class ManagedResponseExceptionMiddleware
{
    private static readonly ActionDescriptor EmptyActionDescriptor = new();

    private static readonly RouteData EmptyRouteData = new();

    private static readonly MediaTypeCollection ContentTypes = new() { "application/problem+json" };

    private static readonly HashSet<string> AllowedHeaderNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.AccessControlAllowCredentials,
            HeaderNames.AccessControlAllowHeaders,
            HeaderNames.AccessControlAllowMethods,
            HeaderNames.AccessControlAllowOrigin,
            HeaderNames.AccessControlExposeHeaders,
            HeaderNames.AccessControlMaxAge,
            HeaderNames.StrictTransportSecurity,
            HeaderNames.WWWAuthenticate,
        };

    private readonly RequestDelegate _next;
    private IActionResultExecutor<ObjectResult> Executor { get; }

    public ManagedResponseExceptionMiddleware(
        RequestDelegate next,
        IActionResultExecutor<ObjectResult> executor
    )
    {
        _next = next;
        Executor = executor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ExceptionDispatchInfo? edi = null;

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            edi = ExceptionDispatchInfo.Capture(exception);
        }

        if (edi != null)
        {
            if (context.Response.HasStarted)
            {
                edi.Throw();
            }

            var error = edi.SourceException;

            var feature = new ExceptionHandlerFeature
            {
                Path = context.Request.Path,
                Error = error,
            };

            ClearResponse(context, StatusCodes.Status500InternalServerError);

            context.Features.Set<IExceptionHandlerPathFeature>(feature);
            context.Features.Set<IExceptionHandlerFeature>(feature);

            var managedException =
                error as ManagedresponseException ?? new ManagedresponseException(error);

            await WriteProblemDetails(context, managedException.ProblemDetails);
        }
    }

    private async Task WriteProblemDetails(HttpContext context, ProblemDetails details)
    {
        var routeData = context.GetRouteData() ?? EmptyRouteData;

        var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

        var result = new ObjectResult(details)
        {
            StatusCode = details.Status ?? context.Response.StatusCode,
            ContentTypes = ContentTypes,
        };

        await Executor.ExecuteAsync(actionContext, result);

        await context.Response.CompleteAsync();
    }

    private static void ClearResponse(HttpContext context, int statusCode)
    {
        var headers = new HeaderDictionary();
        foreach (var header in context.Response.Headers)
        {
            // Because the CORS middleware adds all the headers early in the pipeline,
            // we want to copy over the existing Access-Control-* headers after resetting the response.
            if (AllowedHeaderNames.Contains(header.Key))
            {
                headers.Add(header);
            }
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        foreach (var header in headers)
        {
            context.Response.Headers.Add(header);
        }
    }
}

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
