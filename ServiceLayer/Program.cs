using System.Runtime.InteropServices;
using Topshelf;
using Topshelf.Runtime.DotNetCore;

namespace Organisation.ServiceLayer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HostFactory.Run(x => {
                x.Service<WindowsService>();
                x.SetServiceName("OS2sync");
                x.SetDisplayName("OS2sync");
                x.SetDescription("Synchronization of data to STS Organisation");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    x.UseEnvironmentBuilder(new Topshelf.HostConfigurators.EnvironmentBuilderFactory(c => {
                        return new DotNetCoreEnvironmentBuilder(c);
                    }));
                }
            });
        }
    }
}
