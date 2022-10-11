namespace Irrbloss.Interfaces;

using Microsoft.Extensions.DependencyInjection;

public interface IServiceModule
{
    void AddServices(IServiceCollection service);
}
