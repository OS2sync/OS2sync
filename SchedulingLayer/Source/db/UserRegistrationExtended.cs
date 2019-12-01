using Organisation.BusinessLayer.DTO.Registration;

namespace Organisation.SchedulingLayer
{
    public class UserRegistrationExtended : UserRegistration
    {
        public long Id { get; set; }
        public OperationType Operation { get; set; }
        public string Cvr { get; set; }
    } 
}
