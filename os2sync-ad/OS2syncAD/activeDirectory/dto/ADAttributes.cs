using System.Collections.Generic;

namespace OS2syncAD
{
    public class ADAttributes
    {
        public Dictionary<string, IADAttribute> Attributes { get; }
        public string Uuid { get; set; }
        public string DistinguishedName { get; set; }

        public ADAttributes()
        {
            Attributes = new Dictionary<string, IADAttribute>();
        }

        public IADAttribute GetField(string field)
        {
            foreach (string key in Attributes.Keys)
            {
                if (key.ToLower().Equals(field.ToLower()))
                {
                    return Attributes[key];
                }
            }

            return null;
        }

        public void Add(IADAttribute attribute)
        {
            if (!Contains(attribute.Name))
            {
                Attributes.Add(attribute.Name, attribute);
            }
        }

        public bool Contains(string field)
        {
            foreach (string key in Attributes.Keys)
            {
                if (key.ToLower().Equals(field.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
