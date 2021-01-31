using System;

namespace PackDB.FileSystem.Attributes
{
    public class RetryAttemptsAttribute : Attribute
    {
        public int MaxAttempts { get; set; }
    }
}