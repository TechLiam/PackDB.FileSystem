using System;

namespace PackDB.Core.Locks
{
    public interface ISemaphore
    {
        bool Wait(TimeSpan timeout);
        int Release();
    }
}