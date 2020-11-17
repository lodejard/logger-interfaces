using System;

namespace Microsoft.Extensions.Logging
{
    public class LogInformationAttribute : LogMessageAttribute
    {
        public LogInformationAttribute(string message) : base(LogLevel.Information, message)
        {
        }

        public LogInformationAttribute(int number, string message) : base(number, LogLevel.Information, message)
        {
        }
    }

    public class LogMessageAttribute : Attribute
    {
        public LogMessageAttribute(LogLevel level, string message)
        {
            EventIdNumber = 0;
            Level = level;
            Message = message;
        }
        public LogMessageAttribute(int number, LogLevel level, string message)
        {
            EventIdNumber = number;
            Level = level;
            Message = message;
        }

        public LogLevel Level { get; }
        public int EventIdNumber { get; }
        public string Message { get; }
    }
}