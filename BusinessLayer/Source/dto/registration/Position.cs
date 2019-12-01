using System;

namespace Organisation.BusinessLayer.DTO.Registration
{
    [Serializable]
    public class Position
    {
        public string Name { get; set; }
        public string OrgUnitUuid { get; set; }
    }
}
