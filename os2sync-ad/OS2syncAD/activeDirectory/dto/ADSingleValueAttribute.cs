using System;

namespace OS2syncAD
{
    public class ADSingleValueAttribute : IADAttribute
    {
        public string Value { get; set; }

        public ADSingleValueAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
