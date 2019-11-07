using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RazorPagesContacts.Data;

namespace RazorPagesContacts
{
    enum DbProvider
    {
        InMemory,
        PostgreSQL
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }
        private bool _migrateDatabase = true;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            Logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            (DbProvider dbProvider, string connectionString) = DetermineDatabaseConfiguration();
            switch (dbProvider)
            {
                case DbProvider.PostgreSQL:
                    Logger.LogInformation($"Using PostgreSQL database");
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(connectionString));
                    _migrateDatabase = true;
                    break;
                case DbProvider.InMemory:
                    Logger.LogInformation("Using InMemory database");
                    services.AddDbContext<AppDbContext>(options =>
                              options.UseInMemoryDatabase("name"));
                    _migrateDatabase = false;
                    break;
                default:
                    throw new ArgumentException($"Unknown db provider: {dbProvider}");
            }

            services.AddSingleton<AppConfiguration>(new AppConfiguration
            {
                DatabaseProvider = dbProvider.ToString()
            });

            services.AddRazorPages();

        }

        public void Configure(IApplicationBuilder app)
        {
            if (_migrateDatabase)
            {
                MigrateDatabase(app);
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        private (DbProvider dbProvider, string connectionString) DetermineDatabaseConfiguration()
        {
            DbProvider? dbProvider = Configuration.GetValue<DbProvider?>("DB_PROVIDER");
            string connectionString = Configuration.GetConnectionString("Database");

            // Explicit configuration.
            if (dbProvider != null && connectionString != null)
            {
                return (dbProvider.Value, connectionString);
            }

            // If there is no explicit configuration, try to pick an appropriate one by inspecting environment variables.
            // We support a PostgreSQL database that was created and linked with odo (https://github.com/openshift/odo).

            if (dbProvider == null)
            {
                // 'odo' PostgreSQL has a 'uri' envvar that starts with 'postgres://'.
                string uri = Configuration.GetValue<string>("uri");
                if (uri != null && uri.StartsWith("postgres://"))
                {
                    dbProvider = DbProvider.PostgreSQL;
                }
                else
                {
                    dbProvider = DbProvider.InMemory;
                }
            }

            switch (dbProvider)
            {
                case DbProvider.PostgreSQL:
                    // 'odo' environment variables for PostgreSQL.
                    string database_name = Configuration.GetValue<string>("database_name");
                    string password = Configuration.GetValue<string>("password");
                    Uri uri = Configuration.GetValue<Uri>("uri");
                    string host = uri.Host;
                    int port = uri.Port == -1 ? 5432 : uri.Port;
                    string username = Configuration.GetValue<string>("username");
                    connectionString = $"Host={host};Port={port};Database={database_name};Username={username};Password={password}";
                    break;
                case DbProvider.InMemory:
                    break;
                default:
                    throw new ArgumentException($"Unknown db provider: {dbProvider}");
            }

            return (dbProvider.Value, connectionString);
        }

        private static void MigrateDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<AppDbContext>())
                {
                    context.Database.Migrate();
                }
            }
        }
    }
}