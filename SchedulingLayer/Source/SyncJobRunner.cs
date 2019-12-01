using Quartz;
using Quartz.Impl;
using Organisation.IntegrationLayer;

namespace Organisation.SchedulingLayer
{
    public class SyncJobRunner
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static bool initialized = false;

        public static async void InitAsync()
        {
            if (string.IsNullOrEmpty(OrganisationRegistryProperties.GetInstance().DBConnectionString))
            {
                log.Warn("Not starting scheduler - no connection string configured!");
                return;
            }

            if (!initialized)
            {
                InitDB.InitializeDatabase();

                log.Info("Starting SchedulingLayer");

                // get a scheduler
                IScheduler sched = await new StdSchedulerFactory().GetScheduler();
                await sched.StartDelayed(System.TimeSpan.FromSeconds(30));

                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<SyncJob>()
                    .WithIdentity("syncJob", "syncGroup")
                    .Build();

                // execute updater every minute
                ITrigger trigger = TriggerBuilder.Create()
                  .WithIdentity("syncTrigger", "syncGroup")
                  .StartNow()
                  .WithSimpleSchedule(x => x
                      .WithIntervalInMinutes(1)
                      .RepeatForever())
                  .Build();

                await sched.ScheduleJob(job, trigger);
                initialized = true;
            }
        }
    }
}
