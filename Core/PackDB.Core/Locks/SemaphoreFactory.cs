using System.Diagnostics.CodeAnalysis;

namespace PackDB.Core.Locks
{
    [ExcludeFromCodeCoverage]
    public class SemaphoreFactory : ISemaphoreFactory
    {
        public ISemaphore Create(int initialCount, int maxCount)
        {
            return new SemaphoreProxy(initialCount, maxCount);
        }
    }
}