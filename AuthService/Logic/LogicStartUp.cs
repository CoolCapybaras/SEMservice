using Logic.Users;
using Logic.Users.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Logic;

public static class LogicStartUp
{
    public static IServiceCollection AddLogic(this IServiceCollection services)
    {
        services.AddScoped<IUserManager, UserManager>();

        return services;
    }
}