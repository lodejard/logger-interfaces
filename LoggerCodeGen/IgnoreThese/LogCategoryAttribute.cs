using System;

namespace Microsoft.Extensions.Logging
{
    public class LogCategoryAttribute : Attribute
    {
        public LogCategoryAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}