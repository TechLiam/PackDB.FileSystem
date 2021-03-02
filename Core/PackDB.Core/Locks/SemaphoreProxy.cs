using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace PackDB.Core.Locks
{
    [ExcludeFromCodeCoverage]
    public class SemaphoreProxy : SemaphoreSlim, ISemaphore
    {
        public SemaphoreProxy(int initialCount) : base(initialCount)
        {
        }

        public SemaphoreProxy(int initialCount, int maxCount) : base(initialCount, maxCount)
        {
        }
    }
}