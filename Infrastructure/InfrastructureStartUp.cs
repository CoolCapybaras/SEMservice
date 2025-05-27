using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Interfaces;
using SEM.Infrastructure.Data;
using SEM.Infrastructure.Repositories;


namespace Infrastructure;

public static class InfrastructureStartUp
{
    public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventPostRepository, EventPostRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IInviteRepository, InviteRepository>();

        return services;
    }
}