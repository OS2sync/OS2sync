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

        [JsonIgnore]
        public bool BypassCache { get; set; }
        
        public string Cvr { get; set; }

        internal bool SyncEquals(UserRegistrationExtended other)
        {
            bool fieldsEquals = false, personEquals = false, positionsEquals = false;

            if (this.Operation == other.Operation &&
                string.Equals(this.Cvr, other.Cvr) &&
                string.Equals(this.Uuid, other.Uuid) &&
                string.Equals(this.UserId, other.UserId) &&
                string.Equals(this.PhoneNumber, other.PhoneNumber) &&
                string.Equals(this.Landline, other.Landline) &&
                string.Equals(this.Email, other.Email) &&
                string.Equals(this.RacfID, other.RacfID) &&
                string.Equals(this.Location, other.Location) &&
                string.Equals(this.FMKID, other.FMKID))
            {
                fieldsEquals = true;
            }

            if (string.Equals(this.Person?.Cpr, other.Person?.Cpr) &&
                string.Equals(this.Person?.Name, other.Person?.Name))
            {
                personEquals = true;
            }

            if (this.Positions == null && other.Positions == null)
            {
                positionsEquals = true;
            }
            else if (this.Positions != null && other.Positions != null)
            {
                if (this.Positions.Count == 0 && other.Positions.Count == 0)
                {
                    positionsEquals = true;
                }
                else if (this.Positions.Count == other.Positions.Count)
                {
                    bool misMatch = false;

                    foreach (var thisPosition in this.Positions)
                    {
                        bool found = false;

                        foreach (var otherPosition in other.Positions)
                        {
                            if (string.Equals(thisPosition.Name, otherPosition.Name) &&
                                string.Equals(thisPosition.StartDate, otherPosition.StartDate) &&
                                string.Equals(thisPosition.StopDate, otherPosition.StopDate) &&
                                string.Equals(thisPosition.OrgUnitUuid, otherPosition.OrgUnitUuid))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            misMatch = true;
                            break;
                        }
                    }

                    if (!misMatch)
                    {
                        positionsEquals = true;
                    }
                }
            }

            return fieldsEquals && personEquals && positionsEquals;
        }
    } 
}
