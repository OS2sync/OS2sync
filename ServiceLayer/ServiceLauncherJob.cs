using Quartz;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Net;

namespace Organisation.ServiceLayer
{
    [DisallowConcurrentExecution]
    public class ServiceLauncherJob : IJob
    {
        private bool initialized = false;

        public Task Execute(IJobExecutionContext context)
        {
            if (!initialized)
            {
                initialized = true;

                // Initialize BusinessLayer
                BusinessLayer.Initializer.Init();

                // Initialize SchedulingLayer (if enabled)
                if (IntegrationLayer.OrganisationRegistryProperties.AppSettings.SchedulerSettings.Enabled)
                {
                    SchedulingLayer.SyncJobRunner.InitAsync();
                }

                BuildWebHost().Run();
            }

            return Task.CompletedTask;
        }

        private static IWebHost BuildWebHost()
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls("http://*:5000")
                .UseStartup<Startup>()
                .Build();
        }
    }
}
