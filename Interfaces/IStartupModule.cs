namespace Irrbloss.Interfaces;

using Microsoft.AspNetCore.Routing;

public interface IStartupModule
{
    void AddStartups(IEndpointRouteBuilder app);
}
