using System;
using System.Threading;

namespace Windows10BLEStressTesst
{
    public static class GlobalCounters
    {
        public static int WatchersCreated;
        public static int WatchersStopped;
        public static int AdvertisementsSeen;

        public static void DisplayCounters()
        {
            Console.Write($"\rCreated: {WatchersCreated}, Stopped: {WatchersStopped}, Advertisements: {AdvertisementsSeen}  ");
        }

        public static void IncrementCreated()
        {
            Interlocked.Increment(ref WatchersCreated);
            DisplayCounters();
        }

        public static void IncrementClosed()
        {
            Interlocked.Increment(ref WatchersStopped);
            DisplayCounters();
        }

        public static void IncrementAdvertisementsSeen()
        {
            Interlocked.Increment(ref AdvertisementsSeen);
            DisplayCounters();
        }
    }
}