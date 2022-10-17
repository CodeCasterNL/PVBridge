using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var loggingSection = configuration.GetSection(LoggingConfiguration.SectionName);

            // IOptions<T> registrations
            services.Configure<BridgeConfiguration>(configuration.GetSection(BridgeConfiguration.SectionName));
            services.Configure<LoggingConfiguration>(loggingSection);

            // Add log4Net from configuration file(s).
            services.AddLogging(logging =>
            {
                var options = new Log4NetProviderOptions();
                loggingSection.Bind(LoggingConfiguration.Log4NetSectionName, options);
                logging.AddLog4Net(options);

                logging.AddConfiguration(loggingSection);
#if DEBUG
                logging.AddSimpleConsole(console =>
                {
                    console.IncludeScopes = true;
                    console.SingleLine = true;
                    console.TimestampFormat = "HH:mm:ss ";
                });
                logging.AddDebug();
#endif
            });

            // Dependency injection
            services.AddSingleton<IInputToOutputWriter, InputToOutputWriter>();
            services.AddSingleton(IClock.Default);

            // Register providers with Scrutor, except the caching one. One provider per input/output type. Providers cache clients per configuration.
            services.Scan(s => s.FromApplicationDependencies()
                .AddClasses(classes => 
                    classes.Where(c => c != typeof(CachingSummaryProvider))
                           .AssignableTo<IDataProvider>()
                )
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
            );

            return services;
        }
    }
}
