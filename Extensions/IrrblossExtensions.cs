namespace Irrbloss.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Irrbloss.Interfaces;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public static class IrrblossExtensions
{
    public static IServiceCollection AddServiceModules(this IServiceCollection services)
    {
        var assemblyCatalog = new DependencyContextAssemblyCatalog();
        var assemblies = assemblyCatalog.GetAssemblies();

        var serviceModules = GetModules<IServiceModule>(assemblies);

        foreach (var serviceModule in serviceModules)
        {
            if (Activator.CreateInstance(serviceModule) is not IServiceModule t)
            {
                throw new Exception();
            }

            t.AddServices(services);
        }

        return services;
    }

    public static IServiceCollection AddRouterModules(this IServiceCollection services)
    {
        var assemblyCatalog = new DependencyContextAssemblyCatalog();
        var assemblies = assemblyCatalog.GetAssemblies();

        var routerModules = GetModules<IRouterModule>(assemblies);

        foreach (var routerModule in routerModules)
        {
            services.AddSingleton(typeof(IRouterModule), routerModule);
        }

        return services;
    }

    public static IEndpointRouteBuilder UseRouterModules(this IEndpointRouteBuilder builder)
    {
        foreach (var newMod in builder.ServiceProvider.GetServices<IRouterModule>())
        {
            newMod.AddRoutes(builder);
        }

        return builder;
    }

    public static IEndpointRouteBuilder UseStartupModules(this IEndpointRouteBuilder builder)
    {
        var assemblyCatalog = new DependencyContextAssemblyCatalog();
        var assemblies = assemblyCatalog.GetAssemblies();

        var serviceModules = GetModules<IStartupModule>(assemblies);

        foreach (var serviceModule in serviceModules)
        {
            if (Activator.CreateInstance(serviceModule) is not IStartupModule t)
            {
                throw new Exception();
            }

            t.AddStartups(builder);
        }

        return builder;
    }

    private static IEnumerable<Type> GetModules<T>(IReadOnlyCollection<Assembly> assemblies)
    {
        return assemblies.SelectMany(
            x =>
                x.GetTypes()
                    .Where(
                        t =>
                            !t.IsAbstract
                            && typeof(T).IsAssignableFrom(t)
                            && t != typeof(T)
                            && t.IsPublic
                    )
        );
    }
}
