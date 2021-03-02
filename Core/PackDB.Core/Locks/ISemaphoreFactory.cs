﻿namespace PackDB.Core.Locks
{
    public interface ISemaphoreFactory
    {
        ISemaphore Create(int initialCount, int maxCount);
    }
}