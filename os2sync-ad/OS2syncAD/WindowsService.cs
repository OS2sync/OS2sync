﻿using Quartz;
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
                log.Info("Starting OS2sync service - initializing BusinessLayer");

                // Initialize BusinessLayer
                Organisation.BusinessLayer.Initializer.Init();

                log.Info("Starting OS2sync service - initializing SchedulingLayer");

                // Initialize SchedulingLayer
                Organisation.SchedulingLayer.SyncJobRunner.InitAsync();

                log.Info("Starting OS2sync service - initializing Task Runner");

                // Grab the Scheduler instance from the Factory 
                StdSchedulerFactory factory = new StdSchedulerFactory();
                sched = factory.GetScheduler().Result;
                sched.Start();

                // start AD listener, with a delayed startup of 30 seconds to ensure OS2sync is ready
                IJobDetail job1 = JobBuilder.Create<EventListenerJob>()
                    .WithIdentity("ADListenerJob", "ADListenerGroup")
                    .Build();

                // start 30 seconds after boot, and then run once every 10 seconds
                ITrigger trigger1 = TriggerBuilder.Create()
                    .WithIdentity("ADListenerTrigger", "ADListenerGroup")
                    .StartAt(DateBuilder.FutureDate(30, IntervalUnit.Second))
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever())
                    .ForJob(job1)
                    .Build();

                sched.ScheduleJob(job1, trigger1);

                if (AppConfiguration.CleanupOUJobEnabled)
                {
                    // start OrgUnit Cleanup task
                    IJobDetail job2 = JobBuilder.Create<CleanupOrgUnitJob>()
                        .WithIdentity("CleanupOrgUnitJob", "CleanupOrgUnitGroup")
                        .Build();

                    // start 30 seconds after boot, and then run once every week
                    ITrigger trigger2 = TriggerBuilder.Create()
                        .WithIdentity("CleanupOrgUnitTrigger", "CleanupOrgUnitJob")
                        .WithSchedule(CronScheduleBuilder.CronSchedule(AppConfiguration.CleanOUJobCron))
                        .ForJob(job2)
                        .Build();

                    sched.ScheduleJob(job2, trigger2);
                }

                log.Info("Starting OS2sync service - ready");
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
