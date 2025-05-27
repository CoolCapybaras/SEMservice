using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SEM.Domain.Interfaces;

namespace SEM.Services;

public static class ServicesStartUp
{
    public static IServiceCollection AddServces(this IServiceCollection services)
    {
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventPostService, EventPostService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IInviteService, InviteService>();

        return services;
    }
}