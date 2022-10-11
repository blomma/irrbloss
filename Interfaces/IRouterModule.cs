using Microsoft.AspNetCore.Routing;

namespace Irrbloss.Interfaces;

public interface IRouterModule
{
    void AddRoutes(IEndpointRouteBuilder app);
}
