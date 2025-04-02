using Logic.Interfaces;
using Logic.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Logic;

public static class LogicStartUp
{
    public static IServiceCollection AddLogicDependencies(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileService, UserProfileService>();
        
        return services;
    }
} 