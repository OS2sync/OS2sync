using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Organisation.BusinessLayer.DTO.Read
{
    [Serializable]
    public class Hierarchy
    {
        public List<BasicOU> OUs { get; set; } = new List<BasicOU>();
        public List<BasicUser> Users { get; set; } = new List<BasicUser>();
    }

    [Serializable]
    public class BasicOU
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string ParentOU { get; set; }
    }

    [Serializable]
    public class BasicUser
    {
        public string Uuid { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public List<BasicPosition> Positions { get; set; } = new List<BasicPosition>();
        public string Email { get; set; }
        public string Telephone { get; set; }
        public Status Status { get; set; }

    }

    [Serializable]
    public class BasicPosition
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
    }
}
