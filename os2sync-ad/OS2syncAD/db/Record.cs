using System;

namespace OS2syncAD
{
    public class Record
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
