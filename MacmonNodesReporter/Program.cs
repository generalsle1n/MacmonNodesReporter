using Quartz;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Slack;
using Serilog.Sinks.Slack.Models;
using System.Reflection;

namespace MacmonNodesReporter
{
    public class Program
    {
        private const string _configName = "config.json";
        private const string _logFolder = "Logs";
        private const string _logFileName = "Data.txt";
        public static void Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            Assembly Binary = Assembly.GetEntryAssembly();
            Console.WriteLine(Binary.Location);
            string FolderPath = Path.GetDirectoryName(Binary.Location);
            string LogFolder = Path.Combine(FolderPath, _logFolder);

            if (!Path.Exists(LogFolder))
            {
                Directory.CreateDirectory(LogFolder);
            }

            builder.Configuration.AddJsonFile(Path.Combine(FolderPath, _configName), optional: false, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables(conf =>
            {
                conf.Prefix = "Report";
            });

            Logger Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($@"{LogFolder}\{_logFileName}", rollingInterval: RollingInterval.Month, rollOnFileSizeLimit: true)
                .WriteTo.Slack(new SlackSinkOptions()
                {
                    WebHookUrl = builder.Configuration.GetValue<string>("Log:SlackWebook"),
                    MinimumLogEventLevel = LogEventLevel.Warning,
                })
                .CreateLogger();
            
            builder.Logging.AddSerilog(Logger);
            builder.Services.AddHttpClient();
            builder.Services.AddQuartz(conf =>
            {
                Guid Id = Guid.NewGuid();
                JobKey JobId = new JobKey(Id.ToString());

                conf.AddJob<Worker>(singleJob =>
                {
                    singleJob.WithIdentity(JobId);
                });

                conf.AddTrigger(trigger =>
                {
                    bool HourInterval = builder.Configuration.GetValue<bool>("Interval:LoopIntervalInHours");

                    if (HourInterval)
                    {
                        trigger.WithSimpleSchedule(conf =>
                        {

                            conf.WithIntervalInHours(builder.Configuration.GetValue<int>("Interval:Hours"));
                            conf.WithMisfireHandlingInstructionFireNow();
                            conf.RepeatForever();
                        });
                    }
                    else
                    {
                        int MonthToStart = builder.Configuration.GetValue<int>("Interval:StartDayInMonth");
                        CronScheduleBuilder Schedule = CronScheduleBuilder.MonthlyOnDayAndHourAndMinute(MonthToStart, 0, 0);
                        Schedule.WithMisfireHandlingInstructionFireAndProceed();
                        trigger.WithSchedule(Schedule);
                    }

                    trigger.ForJob(JobId);
                });
            });

            builder.Services.AddQuartzHostedService(conf =>
            {
                conf.WaitForJobsToComplete = true;
            });

            builder.Services.AddWindowsService(conf =>
            {
                conf.ServiceName = "MacmonNodesReporter";
            });

            builder.Services.AddSystemd();


            IHost host = builder.Build();
            host.Run();
        }
    }
}