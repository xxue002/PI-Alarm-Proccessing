using Autofac;
using Core.AlarmProcessor;
using Core.ConnectionManager;
using Core.FileReader;
using Core.Service;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Threading.Tasks;
using Topshelf;

namespace Core
{
    class Program
    {
        private static IContainer _container;

        static Program()
        {
            // Configure and Create a logger using Serilog
            var logger = new LoggerConfiguration().MinimumLevel.Debug()
                                                  .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                                                  .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + "Logs\\History.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: null)
                                                  .CreateLogger();
            
            // Registers instances into container
            var builder = new ContainerBuilder();
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance();
            builder.RegisterType<CsvReader>().As<IReader>().SingleInstance();
            builder.RegisterType<PIConnectionManager>().As<IPIConnectionManager>().SingleInstance();
            builder.RegisterType<AlarmReader>().SingleInstance();
            builder.RegisterType<AlarmService>().As<IAlarmService>().SingleInstance();
            _container = builder.Build();
        }

        static void Main(string[] args)
        {
            var rc = HostFactory.Run(x =>                                   
            {
                x.Service<IAlarmService>(s =>                                   
                {
                    s.ConstructUsing(name => _container.Resolve<IAlarmService>());                
                    s.WhenStarted(al => al.Start());
                    s.WhenStopped(al => al.Stop());
                });

                x.RunAsLocalService();                                       

                x.SetDescription("PI Alarm Processing Service");                  
                x.SetDisplayName("PI Alarm Processing Service");                                  
                x.SetServiceName("PI_RVSALPROCESSOR");                                  
            });                                                            

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }
    }
}
