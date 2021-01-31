using PackDB.FileSystem.Attributes;

namespace PackDB.FileSystem.Tests
{
    [RetryAttempts(MaxAttempts = 3)]
    public class RetryBasicData : BasicData
    {
    }
}