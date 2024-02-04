using Quartz;
using Quartz.Impl;
using Topshelf;
using Organisation.IntegrationLayer;

namespace Organisation.ServiceLayer
{
    public class WindowsService : ServiceControl
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool Start(HostControl hostControl)
        {
            log.Info("OS2sync service is started");

            StdSchedulerFactory factory = new StdSchedulerFactory();
            var sched = factory.GetScheduler().Result;
            sched.Start();

            // ServiceLauncherJob
            IJobDetail serviceLauncherJob = JobBuilder.Create<ServiceLauncherJob>()
                .WithIdentity("serviceLauncherJob", "serviceLauncherGroup")
                .Build();

            ITrigger serviceLauncherTrigger = TriggerBuilder.Create()
                .WithIdentity("serviceLauncherTrigger", "serviceLauncherGroup")
                .StartAt(DateBuilder.FutureDate(5, IntervalUnit.Second))
                .ForJob(serviceLauncherJob)
                .Build();

            sched.ScheduleJob(serviceLauncherJob, serviceLauncherTrigger);

            if (!string.IsNullOrEmpty(OrganisationRegistryProperties.AppSettings.SchedulerSettings.DBConnectionString))
            {
                // CleanupDatabaseJob
                IJobDetail cleanupDatabaseJob = JobBuilder.Create<CleanupDatabaseJob>()
                    .WithIdentity("cleanupDatabaseJob", "cleanupDatabaseGroup")
                    .Build();

                ITrigger cleanupDatabaseStartupTrigger = TriggerBuilder.Create()
                    .WithIdentity("cleanupDatabaseStartupTrigger", "cleanupDatabaseGroup")
                    .StartAt(DateBuilder.FutureDate(15, IntervalUnit.Second))
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(1))
                    .Build();

                sched.ScheduleJob(cleanupDatabaseJob, cleanupDatabaseStartupTrigger);

                ITrigger cleanupDatabaseTrigger = TriggerBuilder.Create()
                    .WithIdentity("cleanupDatabaseTrigger", "cleanupDatabaseGroup")
                    .StartAt(DateBuilder.FutureDate(15, IntervalUnit.Second))
                    .WithSimpleSchedule(x => x
                        .WithIntervalInHours(24)
                        .RepeatForever())
                    .Build();

                sched.ScheduleJob(cleanupDatabaseJob, cleanupDatabaseTrigger);
            }

            // CleanupJob
            IJobDetail cleanupJob = JobBuilder.Create<CleanupJob>()
                .WithIdentity("cleanupJob", "cleanupGroup")
                .Build();

            // execute updater every minute
            ITrigger cleanupTrigger = TriggerBuilder.Create()
                .WithIdentity("cleanupTrigger", "cleanupGroup")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(1)
                    .RepeatForever())
                .Build();

            sched.ScheduleJob(cleanupJob, cleanupTrigger);


            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            log.Info("OS2sync service is stopped");

            return true;
        }
    }
}
