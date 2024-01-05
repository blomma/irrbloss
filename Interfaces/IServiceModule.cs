using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Irrbloss.Interfaces;

public interface IServiceModule
{
    void AddServices(IServiceCollection service, IConfiguration configuration);
}
