using System;
using System.Runtime.InteropServices;
using Topshelf;
using Topshelf.Runtime.DotNetCore;

namespace OS2syncAD
{
    class Program
    {
        public static void Main(String[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<WindowsService>();
                x.SetServiceName("OS2sync AD Listener");
                x.SetDisplayName("OS2sync AD Listener");
                x.SetDescription("Acts as a bridge between Active Directory and OS2sync");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    x.UseEnvironmentBuilder(new Topshelf.HostConfigurators.EnvironmentBuilderFactory(c =>
                    {
                        return new DotNetCoreEnvironmentBuilder(c);
                    }));
                }
            });
        }
    }
}
