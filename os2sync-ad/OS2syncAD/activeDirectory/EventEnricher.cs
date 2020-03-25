using System;

namespace OS2syncAD
{
    public class EventEnricher
    {
        private ADAttributeLoader attributeLoader;
        private ADUtils adUtils;

        public EventEnricher(ADAttributeLoader attributeLoader, ADUtils adUtils)
        {
            this.attributeLoader = attributeLoader;
            this.adUtils = adUtils;
        }

        public ADEvent Enrich(ADEvent poorEvent)
        {
            ObjectType affectedObjectType = poorEvent.AffectedObjectType;
            OperationType operationType = poorEvent.OperationType;
            DateTime timeOcurred = poorEvent.TimeOcurred;
            long id = poorEvent.Id;
            ADAttributes attributes = null;

            if (poorEvent.OperationType.Equals(OperationType.Remove))
            {
                attributes = new ADAttributes();
                attributes.Uuid = poorEvent.ADAttributes.Uuid;
                attributes.DistinguishedName = poorEvent.ADAttributes.DistinguishedName;

                return new ADEvent(id, operationType, affectedObjectType, attributes, timeOcurred, null);
            }

            // make an ldap call and get full object
            attributes = attributeLoader.Load(poorEvent.ADAttributes.DistinguishedName);
            if (string.IsNullOrEmpty(attributes.Uuid))
            {
                // if the object is outside the hiearchy, the UUID is nulled, so for updates
                // we need to ensure we get the UUID copied back, so we can safely delete the object
                attributes.Uuid = poorEvent.ADAttributes.Uuid;
            }

            string parentOUUUID = adUtils.GetOUParent(poorEvent)?.ADAttributes?.Uuid;

            return new ADEvent(id, operationType, affectedObjectType, attributes, timeOcurred, parentOUUUID);
        }
    }
}
