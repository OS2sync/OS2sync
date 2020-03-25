
using System;
using System.Collections.Generic;

namespace OS2syncAD
{
    public class ADMultiValueAttribute : IADAttribute
    {
        public List<string> Values { get; }

        public ADMultiValueAttribute(string name)
        {
            Name = name;
            Values = new List<string>();
        }

        public void Add(ICollection<String> attributes)
        {
            foreach (string attribute in attributes)
            {
                Values.Add(attribute);
            }
        }
    }
}
