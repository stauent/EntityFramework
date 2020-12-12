using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.IO;


namespace ConfigurationAssistant
{
    /// <summary>
    /// Details on how the app "TApp" was created and configured
    /// </summary>
    /// <typeparam name="TApp">The type of the application that was configured</typeparam>
    public class ConfigurationResults<TApp> where TApp : class 
    {
        public IHostBuilder builder { get; set; }
        public IHost myHost { get; set; }

        public TApp myService { get; set; }

        public ST GetService<ST>()
        {
            return myHost.Services.GetService<ST>();
        }
    }

    /// <summary>
    /// This class will configure a console application to use Dependency Injection and support console and debug logging
    /// </summary>
    public static class ConsoleHostBuilderHelper
    {
        public delegate void ConfigureLocalServices<T>(HostBuilderContext hostingContext, IServiceCollection services) where T : class;


        public static IHostBuilder CreateHostBuilder<TApp>(string[] args, ConfigureLocalServices<TApp> localServiceConfiguration = null) where TApp : class
        {
            IUserConfiguration userConfiguration = ConfigFactory.Initialize<TApp>();

            IHostBuilder hostBuilder =  Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostingContext, services) =>
                {
                    localServiceConfiguration?.Invoke(hostingContext, services);

                    services
                        .AddTransient<TApp>()
                        .AddSingleton<IApplicationRequirements<TApp>, ApplicationRequirements<TApp>>()
                        .AddSingleton<IUserConfiguration> (sp =>
                                {
                                    return (userConfiguration);
                                })
                        .BuildServiceProvider();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    ConfigureCustomLogging(hostingContext, logging, userConfiguration);
                });

            return (hostBuilder);
        }


        public static void ConfigureCustomLogging(HostBuilderContext hostingContext, ILoggingBuilder logging, IUserConfiguration userConfiguration)
        {
            logging.ClearProviders();

            if (userConfiguration.IsLoggingEnabled)
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));

                if (userConfiguration.IsLoggerEnabled(EnabledLoggersEnum.Debug))
                    logging.AddDebug();

                if (userConfiguration.IsLoggerEnabled(EnabledLoggersEnum.Console))
                    logging.AddConsole();

                if (userConfiguration.IsLoggerEnabled(EnabledLoggersEnum.File))
                {
                    // Must set the log name prior to adding Log4Net because it must know this value
                    // before it loads the config file. It does pattern matching and substitution on the filename.
                    string logName = $"{userConfiguration.LogName}.log";
                    if (!string.IsNullOrEmpty(userConfiguration.LogPath))
                    {
                        if (!Directory.Exists(userConfiguration.LogPath))
                        {
                            Directory.CreateDirectory(userConfiguration.LogPath);
                        }

                        logName = $"{userConfiguration.LogPath}\\{logName}";
                    }
                    log4net.GlobalContext.Properties["LogName"] = logName;
                    logging.AddLog4Net("log4net.config");
                }
            }
        }

        /// <summary>
        /// Creates IOC container for console apps. Injects logging and custom configuration
        /// for application to consume in its constructor. The following example shows how
        /// to launch a class called "MyApplication" as your main application. It's constructor
        /// will have logging and configuration injected into it.
        ///
        ///             configuredApplication = ConsoleHostBuilderHelper.CreateApp<MyApplication>(args);
        ///             await configuredApplication.myService.Run();
        /// 
        /// </summary>
        /// <typeparam name="TApp">Type of your main application class</typeparam>
        /// <param name="args">Any command line parameters you used to launch the console app are passed here</param>
        /// <param name="localServiceConfiguration">Delegate you can use to add more services to the IOC container</param>
        /// <returns>An ConfigurationResults object containing all the information about how this application is hosted</returns>
        public static ConfigurationResults<TApp> CreateApp<TApp>(string[] args, ConfigureLocalServices<TApp> localServiceConfiguration = null) where TApp : class
        {
            ConfigurationResults<TApp> config = new ConfigurationResults<TApp>();
            config.builder = CreateHostBuilder<TApp>(args, localServiceConfiguration);
            config.myHost = config.builder.Build();
            config.myService = config.myHost.Services.GetRequiredService<TApp>();
            return (config);
        }

        public static IApplicationRequirements<TApp> CreateApplicationRequirements<TApp>(string[] args, ConfigureLocalServices<TApp> localServiceConfiguration = null) where TApp : class
        {
            ConfigurationResults<TApp> config = new ConfigurationResults<TApp>();
            config.builder = CreateHostBuilder<TApp>(args, localServiceConfiguration);
            config.myHost = config.builder.Build();
            IApplicationRequirements<TApp> requirements = config.myHost.Services.GetRequiredService<IApplicationRequirements<TApp>>();
            return (requirements);
        }
    }
}
