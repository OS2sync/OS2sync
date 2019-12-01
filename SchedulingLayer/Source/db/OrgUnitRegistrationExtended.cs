using Organisation.BusinessLayer.DTO.Registration;

namespace Organisation.SchedulingLayer
{
    public class OrgUnitRegistrationExtended : OrgUnitRegistration
    {
        public long Id { get; set; }
        public OperationType Operation { get; set; }
        public string Cvr { get; set; }
    }
}
