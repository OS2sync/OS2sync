using System;

namespace Organisation.IntegrationLayer
{
    internal class Health
    {
        private BrugerStub stub = new BrugerStub();

        public bool AreServicesReachable()
        {
            return stub.IsAlive();
        }
    }
}
