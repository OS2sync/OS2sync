using Organisation.IntegrationLayer;
using System.Runtime.CompilerServices;

namespace Organisation.BusinessLayer
{
    public static class Initializer
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Init()
        {
            OrganisationRegistryProperties.GetInstance();
        }
    }
}
