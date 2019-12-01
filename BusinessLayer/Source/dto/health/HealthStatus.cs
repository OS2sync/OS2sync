using System;

namespace Organisation.BusinessLayer.DTO.Health
{
    [Serializable]
    public class HealthStatus
    {
        public bool ServiceStatus { get; set; } = true;
        public bool DBStatus { get; set; } = true;

        public bool Up()
        {
            return ServiceStatus && DBStatus;
        }
    }
}
