using System;

namespace OS2syncAD
{
    public class ADEvent
    {
        public long Id { get; set; }
        public OperationType OperationType { get; set; }
        public ObjectType AffectedObjectType { get; set; }
        public ADAttributes ADAttributes { get; set; }
        public DateTime TimeOcurred { get; set; }
        public string ParentOUUUID { get; set; }

        public ADEvent(long id, OperationType operationType, ObjectType affectedObjectType, ADAttributes adAttributes, DateTime timeOcurred, string parentOUUUID)
        {
            this.Id = id;
            this.OperationType = operationType;
            this.AffectedObjectType = affectedObjectType;
            this.ADAttributes = adAttributes;
            this.TimeOcurred = timeOcurred;
            this.ParentOUUUID = parentOUUUID;
        }
    }

    public enum ObjectType
    {
        User,
        OU
    }

    public enum OperationType
    {
        Create,
        Update,
        Remove
    }
}
