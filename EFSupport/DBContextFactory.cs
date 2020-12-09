using System;
using Microsoft.EntityFrameworkCore;
using ConfigurationAssistant;
using Microsoft.EntityFrameworkCore.Proxies;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace EFSupport
{
    public static class DBContextFactory
    {
        /// <summary>
        /// e.g. AddDbContextScoped SchoolContext
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        /// <param name="UseLazyLoading"></param>
        public static void AddDbContextScoped<TImplementation>(this IServiceCollection services, bool UseLazyLoading = true) where TImplementation : DbContext
        {
            services.AddDbContext<TImplementation>(options =>
            {
                try
                {
                    // The ConfigFactory static constructor reads the "MyProjectSettings" from appsettings.json
                    // or secrets.json and exposes the IUserConfiguration interface. We use that interface
                    // to retrieve the connection string mapped to the DatabaseName.
                    IUserConfiguration userConfiguration = ConfigFactory.Initialize<TImplementation>();
                    string databaseName = typeof(TImplementation).Name.Replace("Context", "");
                    string connectionString = userConfiguration?.ConnectionString(databaseName);

                    if (UseLazyLoading)
                        options.UseLazyLoadingProxies();

                    options.UseSqlServer(connectionString);

                }
                catch { }
            }, ServiceLifetime.Scoped);

        }

        /// <summary>
        /// Creates a DbContextOptions object of type T
        /// </summary>
        /// <typeparam name="T">Type of DbContext to create options for</typeparam>
        /// <param name="DatabaseName">Name of the database whose connection string we are to use</param>
        /// <param name="UseLazyLoading">If true, lazy loading will be used</param>
        /// <returns>A live DbContextOptions of type T</returns>
        public static DbContextOptions<T> GetDbContextOptions<T>(string DatabaseName, bool UseLazyLoading = true) where T : DbContext
        {
            DbContextOptionsBuilder<T> optionsBuilder = new DbContextOptionsBuilder<T>();

            try
            {
                // The ConfigFactory static constructor reads the "MyProjectSettings" from appsettings.json
                // or secrets.json and exposes the IUserConfiguration interface. We use that interface
                // to retreive the connection string mapped to the DatabaseName.
                IUserConfiguration userConfiguration = ConfigFactory.Initialize<T>();
                string ConnectionString = userConfiguration?.ConnectionString(DatabaseName);

                if(UseLazyLoading)
                    optionsBuilder.UseLazyLoadingProxies();

                optionsBuilder.UseSqlServer(ConnectionString);

            }
            catch { }

            return (optionsBuilder.Options);
        }

        /// <summary>
        /// Returns a DbContext based on the name of the context type "T"
        /// </summary>
        /// <typeparam name="T">DbContext derived object type</typeparam>
        /// <param name="DatabaseName">Name of the database whose connection string we are to use</param>
        /// <param name="UseLazyLoading">If true, lazy loading will be used</param>
        /// <returns>A live DbContext derived object</returns>
        public static T GetDbContext<T>(string DatabaseName, bool UseLazyLoading = true) where T : DbContext
        {
            DbContextOptions<T> options = DBContextFactory.GetDbContextOptions<T>(DatabaseName);
            T db = (T)Activator.CreateInstance(typeof(T), options);
            return (db);
        }

        /// <summary>
        /// Returns a DbContext based on the name of the context type "T"
        /// </summary>
        /// <typeparam name="T">The word "Context" will be removed from the typename and the rest will be used as the database name</typeparam>
        /// <param name="UseLazyLoading">If true, lazy loading will be used</param>
        /// <returns>A live DbContext derived object</returns>
        public static T GetDbContext<T>(bool UseLazyLoading = true) where T : DbContext
        {
            string DatabaseName = typeof(T).Name.Replace("Context", "");
            return (GetDbContext<T>(DatabaseName, UseLazyLoading));
        }

        public static T GetMigrationsDbContext<T>() where T : DbContext
        {
            T db = (T)Activator.CreateInstance(typeof(T));
            return (db);
        }


        /// <summary>
        /// Extension method to derive the database name from the DbContext type
        /// </summary>
        /// <typeparam name="T">Type of DbContext</typeparam>
        /// <param name="TheContext">Reference being extended</param>
        /// <returns></returns>
        public static string DBNameFromContext<T>(this T TheContext) where T:DbContext
        {
            string DatabaseName = typeof(T).Name.Replace("Context", "");
            return (DatabaseName);
        }

        public static DbContextOptionsBuilder ConfigureSqlServer<T>(this DbContextOptionsBuilder optionsBuilder) where T : DbContext
        {
            if (!optionsBuilder.IsConfigured)
            {
                IUserConfiguration userConfiguration = ConfigFactory.Initialize<T>();
                string DatabaseName = typeof(T).Name.Replace("Context", "");
                optionsBuilder.UseSqlServer(userConfiguration.ConnectionString(DatabaseName));
            }

            return (optionsBuilder);
        }

        public static DbContextOptionsBuilder ConfigureSqlServer<T>(this T dbContext, DbContextOptionsBuilder optionsBuilder) where T : DbContext
        {
            object[] args = { };

            Type o = typeof(ConfigFactory);

            try
            {
                MethodInfo mi = o.GetMethod("GenericInitialize");
                MethodInfo miConstructed = mi.MakeGenericMethod(typeof(T));
                IUserConfiguration userConfiguration = (IUserConfiguration)miConstructed.Invoke(dbContext, args);

                string DatabaseName = dbContext.DBNameFromContext();
                optionsBuilder.UseSqlServer(userConfiguration.ConnectionString(DatabaseName));

            }
            catch (Exception e)
            {
            }

            return (optionsBuilder);
        }

    }
}
