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
                if (IntegrationLayer.OrganisationRegistryProperties.GetInstance().EnableScheduler)
                {
                    SchedulingLayer.SyncJobRunner.InitAsync();
                }

                BuildWebHost().Run();
            }

            return Task.CompletedTask;
        }

        private static IWebHost BuildWebHost()
        {
            if (IntegrationLayer.OrganisationRegistryProperties.GetInstance().SslEnabled)
            {
                return new WebHostBuilder()
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 5000, listenOptions =>
                        {
                            listenOptions.UseHttps(IntegrationLayer.OrganisationRegistryProperties.GetInstance().SslKeystorePath, IntegrationLayer.OrganisationRegistryProperties.GetInstance().SslKeystorePassword);
                        });
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>()
                    .Build();
            }

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
