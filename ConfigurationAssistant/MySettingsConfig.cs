using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace ConfigurationAssistant
{
    public interface IUserConfiguration
    {
        /// <summary>
        /// Name of user using the application 
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Api Key used for HMAC authentication
        /// </summary>
        string ApiKey { get; set; }

        /// <summary>
        /// Secret key used for HMAC authentication
        /// </summary>
        string ApiSecret { get; set; }

        /// <summary>
        /// Returns the connection string associated with the "DatabaseName"
        /// </summary>
        /// <param name="DatabaseName">Name of database we want to get the connection string for</param>
        /// <returns>Connection string associated with the specified DatabaseName</returns>
        string ConnectionString(string DatabaseName);
    }

    internal class SettingsConfig : IUserConfiguration
    {
        /// <summary>
        /// Name of user using the application 
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Api Key used for HMAC authentication
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Secret key used for HMAC authentication
        /// </summary
        public string ApiSecret { get; set; }

        /// <summary>
        /// Every Name:Value pair in the MyProjectSettings:ConnectionStrings
        /// appsettings is deserialized into this list.
        /// </summary
        public List<DbContextConnectionStrings> ConnectionStrings { get; set; }

        /// <summary>
        /// Returns the connection string associated with the "DatabaseName"
        /// </summary>
        /// <param name="DatabaseName">Name of database we want to get the connection string for</param>
        /// <returns>Connection string associated with the specified DatabaseName</returns>
        public string ConnectionString(string ConnectionName)
        {
            DbContextConnectionStrings found = ConnectionStrings?.Find(item => item.Name == ConnectionName);
            return (found?.Value);
        }
    }

    /// <summary>
    /// Each connection string entry in the appsettings.json file is represented by a Json object
    /// that has the properties "Name" and "Value". The configuration file has a property called 
    /// "ConnectionStrings" that contains a JSON array of these items.
    /// </summary>
    internal class DbContextConnectionStrings
    {
        /// <summary>
        /// Name of the database
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Connection string value
        /// </summary>
        public string Value { get; set; }
    }


    // This application uses user-secrets to hide configuration settings.
    // Values that are NOT secret can be stored in the appsettings.json file in the open.
    // Values that ARE SECRET and should not be known to anyone are stored in a user-secret. Read the following
    // article to understand the process.
    // How to use user-secrets https://www.infoworld.com/article/3576292/how-to-work-with-user-secrets-in-asp-net-core.html
    // HMAC authentication: https://bitoftech.net/2014/12/15/secure-asp-net-web-api-using-api-key-authentication-hmac-authentication/
    public static class ConfigFactory
    {
        private static object locker = new object();
        private static Dictionary<string, IUserConfiguration> _userConfiguration = new Dictionary<string, IUserConfiguration>();

        private static void AddUserConfiguration(string AssemblyName, IUserConfiguration UserConfiguration)
        {
            lock (locker)
            {
                // Ensure key doesn't already exist
                if(!_userConfiguration.ContainsKey(AssemblyName))
                    _userConfiguration.Add(AssemblyName, UserConfiguration);
            }
        }

        private static IUserConfiguration FindConfiguration(string AssemblyName)
        {
            IUserConfiguration userConfiguration = null;
            lock (locker)
            {
                if (_userConfiguration.ContainsKey(AssemblyName))
                    userConfiguration = _userConfiguration[AssemblyName];
            }

            return (userConfiguration);
        }

        public static IUserConfiguration GenericInitialize<T>() where T : class
        {
            return Initialize<T>();
        }

        /// <summary>
        /// Provides the user the opportunity to initialize the user configuration
        /// based on a type T that exists in the same assembly as where the user secret
        /// "UserSecretsId" section exists in the .csproj file
        /// </summary>
        /// <typeparam name="T">A type that exists in the user secret assembly</typeparam>
        /// <returns>Configured IUserConfiguration</returns>
        public static IUserConfiguration Initialize<T>() where T : class
        {
            Assembly CurrentAssembly = typeof(T).GetTypeInfo().Assembly;
            return (Initialize(CurrentAssembly));
        }


        /// <summary>
        /// Provides the user the opportunity to initialize the user configuration
        /// </summary>
        /// <param name="CurrentAssembly">The assembly where the "UserSecretsId" exists in the .csproj file</param>
        /// <returns>Configured IUserConfiguration</returns>
        public static IUserConfiguration Initialize(Assembly CurrentAssembly = null)
        {
            IUserConfiguration retVal = null;

            // If the user did not specify the assembly that contains the "UserSecretsId" configuration
            // then assume its the entry assembly
            if (CurrentAssembly == null)
                CurrentAssembly = Assembly.GetEntryAssembly();

            string AssemblyName = CurrentAssembly.FullName;
            retVal = FindConfiguration(AssemblyName);
            if (retVal == null)
            {
                // NOTE: The order in which we add to the configuration builder will
                //       determine the order of override. So in this case the settings
                //       in the "appsettings.json" file are used first, if a user-secret
                //       with the same name is provided then it will override the value
                //       in the .json file. And finally, if an environment variable
                //       with the same name is found then it will override the user-secret.
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                // The appsettings will determine if we want to use user secrets and/or environment variables
                var initialConfig = builder.Build();
                bool UseUserSecrets = initialConfig.GetValue<bool>("UseUserSecrets");
                bool UseEnvironment = initialConfig.GetValue<bool>("UseEnvironment");
                RuntimeEnvironment = initialConfig.GetValue<string>("RuntimeEnvironment");

                // Override appsettings.json properties with user secrets and/or environment variables.
                // The UseUserSecrets and UseEnvironment settings in appsettings.json will determine
                // if these overrides are applied or not.
                if (UseUserSecrets)
                {
                    try
                    {
                        builder.AddUserSecrets(CurrentAssembly);
                    }
                    catch (Exception Err)
                    {
                    }
                }

                if (UseEnvironment)
                {
                    builder.AddEnvironmentVariables();
                }

                // Build the final configuration
                IConfigurationRoot configuration = builder.Build();

                // Bind the configuration properties to the properties in the SettingsConfig object
                IConfigurationSection myConfiguration = configuration.GetSection("MyProjectSettings");
                retVal = new SettingsConfig();
                myConfiguration.Bind(retVal);

                // Save the configuration so we don't have to create it again
                AddUserConfiguration(AssemblyName, retVal);
            }

            return (retVal);

        }

        /// <summary>
        /// Returns the name of the environment we are currently running in
        /// </summary>
        public static string RuntimeEnvironment = "unknown";

        /// <summary>
        /// Returns the connection string for the specified database context
        /// </summary>
        /// <typeparam name="T">DbContext derived class</typeparam>
        /// <returns>Connection string associated with the DbContext</returns>
        public static string ConnectionString<T>() where T : class
        {
            IUserConfiguration config = Initialize<T>();
            string DatabaseName = typeof(T).Name.Replace("Context", "");
            return config.ConnectionString(DatabaseName);
        }   
    }
}
