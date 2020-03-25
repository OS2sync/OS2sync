
using System;

namespace OS2syncAD
{
    public class ADNullValueAttribute : IADAttribute
    {
        public string Value { get; set; }

        public ADNullValueAttribute(string name)
        {
            Name = name;
        }
    }
}
