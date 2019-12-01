using Quartz;
using Quartz.Impl;
using Topshelf;

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

            IJobDetail job = JobBuilder.Create<ServiceLauncherJob>()
                .WithIdentity("serviceLauncherJob", "serviceLauncherGroup")
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("serviceLauncherTrigger", "serviceLauncherGroup")
                .StartAt(DateBuilder.FutureDate(5, IntervalUnit.Second))
                .ForJob(job)
                .Build();

            sched.ScheduleJob(job, trigger);

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            log.Info("OS2sync service is stopped");

            return true;
        }
    }
}
