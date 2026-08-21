using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Data;

namespace CardDemo.OnlineTransactionManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCardDemoOnlineTransactionManagement(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OnlineTransactionManagementDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}
