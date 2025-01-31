﻿using System;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaArchiver;
using MediaArchiver.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

namespace ConsoleApp1
{
    class Program
    {
        public static IConfigurationRoot Configuration;
        static async Task Main(string[] args)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Create service provider
            var builder = new HostBuilder().ConfigureServices(collection =>
            {
                foreach (var serviceDescriptor in serviceCollection)
                {
                    collection.Add(serviceDescriptor);
                }
            });
            await builder.RunConsoleAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Build configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Add access to generic IConfigurationRoot
            services.AddSingleton<IConfigurationRoot>(Configuration);

            //logging
            services.AddSingleton<Serilog.ILogger>(provider =>
            {
                var logger = new LoggerConfiguration().WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                    .CreateLogger();
                return logger;
            });

            services.AddSingleton<ILoggerFactory>(provider =>
            {
                var logger = provider.GetService<Serilog.ILogger>();
                var factory = new LoggerFactory();
                return factory.AddSerilog(logger);
            });
            Func<string,string> temp = (string a) => { return a; };

            services.AddSingleton(provider =>
            {
                return new Func<string,IHashStore>((string dbFileName) =>
                {
                    return SingletonFactory<DbHashStore>.GetSingleton(dbFileName, new Func<DbHashStore>(() =>
                    {
                        return new DbHashStore("sourceHashs", Path.Combine(Configuration["HashDatabases"], dbFileName),
                            provider.GetService<ILogger>());
                    }));

                });
            });


            services.AddTransient<MediaArchiver.IMediaReader>(provider =>
                {
                    var sourceDir = new DirectoryInfo(Configuration["SourceDirectory"]);
                    var useFastReader = Boolean.Parse(Configuration["useFastReader"] ?? "false");
                    var logger = provider.GetService<Serilog.ILogger>();

                    if (useFastReader)
                    {
                        var sourceDb = provider.GetService<Func<string, IHashStore>>().Invoke("source.db");
                        return new FastMediaReader(sourceDb, sourceDir, logger);
                    }

                    return new MediaReader(sourceDir, logger);
                }
            );

                // Add app
                services.AddTransient<MediaArchiver.App>(provider =>
            {
                // add services for app
                var targetDir = new DirectoryInfo(Configuration["ArchiveDirectory"]);
                var sourceDb = provider.GetService<Func<string, IHashStore>>().Invoke("source.db");
                var targetDb = provider.GetService<Func<string, IHashStore>>().Invoke("target.db");

                return new App(targetDir, sourceDb, targetDb, provider.GetService<IMediaReader>(),
                    provider.GetService<ILogger>());
            });
            // Add periodic service
            services.AddHostedService<TimerArchiveService>(provider =>
            {
                var serviceInterval = Int32.Parse(Configuration["serviceIntervalInHours"] ?? "3"); 
                
                return new TimerArchiveService(provider.GetService<App>(), provider.GetService<Serilog.ILogger>(), TimeSpan.FromHours(serviceInterval));
            });
        }
    }
}
