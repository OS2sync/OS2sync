using Organisation.BusinessLayer.DTO.Registration;
using System.Text.Json.Serialization;

namespace Organisation.SchedulingLayer
{
    public class UserRegistrationExtended : UserRegistration
    {
        [JsonIgnore]
        public long Id { get; set; }
        [JsonIgnore]
        public OperationType Operation { get; set; }
        public string Cvr { get; set; }
    } 
}
