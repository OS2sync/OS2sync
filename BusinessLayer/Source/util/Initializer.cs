using Organisation.IntegrationLayer;
using System.Net;
using System.Runtime.CompilerServices;

namespace Organisation.BusinessLayer
{
    public static class Initializer
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Init()
        {
            // set TLS version to 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            OrganisationRegistryProperties.GetInstance();
        }
    }
}
