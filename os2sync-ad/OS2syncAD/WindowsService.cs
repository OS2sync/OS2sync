using Quartz;
using Quartz.Impl;
using Topshelf;

namespace OS2syncAD
{
    public class WindowsService : ServiceControl
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private IScheduler sched;

        public bool Start(HostControl hostControl)
        {
            try
            {
                // Initialize BusinessLayer
                Organisation.BusinessLayer.Initializer.Init();

                // start scheduler
                Organisation.SchedulingLayer.SyncJobRunner.InitAsync();

                // Grab the Scheduler instance from the Factory 
                StdSchedulerFactory factory = new StdSchedulerFactory();
                sched = factory.GetScheduler().Result;
                sched.Start();

                // start AD listener, with a delayed startup of 30 seconds to ensure OS2sync is ready
                IJobDetail job = JobBuilder.Create<EventListenerJob>()
                    .WithIdentity("ADListenerJob", "ADListenerGroup")
                    .Build();

                // start 30 seconds after boot, and then run once every 10 seconds
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("ADListenerTrigger", "ADListenerGroup")
                    .StartAt(DateBuilder.FutureDate(30, IntervalUnit.Second))
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever())
                    .ForJob(job)
                    .Build();

                sched.ScheduleJob(job, trigger);
            }
            catch (SchedulerException se)
            {
                log.Error(se);

                return false;
            }

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            sched.Shutdown();

            return true;
        }
    }
}
