namespace Irrbloss.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public interface IServiceModule
{
    void AddServices(IServiceCollection service, IConfiguration configuration);
}
