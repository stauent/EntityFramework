using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Linq.Expressions;

using ConfigurationAssistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ConfigurationAssistant
{
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
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostingContext, services) =>
                {
                    localServiceConfiguration?.Invoke(hostingContext, services);

                    services
                        .AddTransient<TApp>()
                        .AddTransient(typeof(IStaticConfigFactory<>), typeof(StaticConfigFactory<>))
                        .BuildServiceProvider();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                });
        }

        public static ConfigurationResults<TApp> CreateApp<TApp>(string[] args, ConfigureLocalServices<TApp> localServiceConfiguration = null) where TApp : class
        {

            ConfigurationResults<TApp> config = new ConfigurationResults<TApp>();
            config.builder = CreateHostBuilder<TApp>(args, localServiceConfiguration);
            config.myHost = config.builder.Build();
            config.myService = config.myHost.Services.GetRequiredService<TApp>();
            return (config);
        }

    }

}
