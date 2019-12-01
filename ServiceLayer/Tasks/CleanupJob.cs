using Quartz;
using Quartz.Impl;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Organisation.ServiceLayer
{
    [DisallowConcurrentExecution]
    public class CleanupJob : IJob
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static bool initialized = false;

        public static async void InitAsync()
        {
            if (!initialized)
            {
                log.Info("Starting Cleanup Job");

                NameValueCollection properties = new NameValueCollection
                {
                    // json serialization is the one supported under .NET Core (binary isn't)
                    ["quartz.serializer.type"] = "json",

                    // the following setup of job store is just for example and it didn't change from v2
                    ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz"
                };
                // get a scheduler
                IScheduler sched = await new StdSchedulerFactory(properties).GetScheduler();
                await sched.Start();

                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<CleanupJob>()
                    .WithIdentity("cleanupJob", "cleanupGroup")
                    .Build();

                // execute updater every minute
                ITrigger trigger = TriggerBuilder.Create()
                  .WithIdentity("cleanupTrigger", "cleanupGroup")
                  .StartNow()
                  .WithSimpleSchedule(x => x
                      .WithIntervalInMinutes(1)
                      .RepeatForever())
                  .Build();

                await sched.DeleteJob(job.Key); // delete the job if it's already there from some previous execution
                await sched.ScheduleJob(job, trigger);
                initialized = true;
            }
        }

        public async Task Execute(IJobExecutionContext context) => await Task.Run(() => 
        {
            HierarchyController.Cleanup();
        });
    }
}