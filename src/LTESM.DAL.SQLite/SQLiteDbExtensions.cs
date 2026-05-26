using LTESM.DAL.Abstractions;
using LTESM.DAL.SQLite.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LTESM.DAL.SQLite;

public static class SQLiteDbExtensions
{
    public static IServiceCollection AddSQLiteDbContext(
        this IServiceCollection services,
        DatabaseConfiguration databaseSettings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseSettings.ConnectionString);

        services.AddDbContext<ILTEDbContext, SQLiteDbContext>(options =>
        {
            options.UseSqlite(databaseSettings.ConnectionString);

            if (databaseSettings.LoggingEnabled)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }
}
