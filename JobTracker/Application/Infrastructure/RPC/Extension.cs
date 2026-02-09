using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace JobTracker.Application.Infrastructure.RPC;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRpcSystem(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();

        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(IRpcHandler).IsAssignableFrom(t));

        foreach (var handlerType in handlerTypes)
        {
            services.AddSingleton(typeof(IRpcHandler), handlerType);

            var ctor = handlerType.GetConstructors().FirstOrDefault();
            if (ctor == null) continue;

            foreach (var param in ctor.GetParameters())
            {
                var paramType = param.ParameterType;

                // Skip if already registered
                if (services.Any(sd => sd.ServiceType == paramType)) continue;

                // Skip framework services that are provided by the DI container
                if (paramType == typeof(IServiceProvider)) continue;

                // Register HttpClient types via typed client
                if (paramType == typeof(HttpClient))
                {
                    services.AddHttpClient(paramType.Name);
                }
                else
                {
                    services.AddSingleton(paramType);
                }
            }
        }

        return services;
    }
}
