using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Windows10BLEStressTesst
{
    public class Program
    {
        public static int NumberOfThreads = 10;
        public static int NumberOfWatchers = 10;
        public static TimeSpan WatcherLifespan = TimeSpan.FromMinutes(1);

        static void Main(string[] args)
        {
            var cli = new OptionSet()
            {
                {"t=", "Number of threads to create.", v => NumberOfThreads = Int32.Parse(v)},
                {"w=", "Number of watchers per thread to create.", v => NumberOfWatchers = Int32.Parse(v)},
            };

            cli.Parse(args);

            Console.WriteLine($"Creating {NumberOfThreads} each with {NumberOfWatchers} watchers.");

            for (var i = 0; i < NumberOfThreads; i++)
            {
                new Thread(() =>
                {
                    while (true)
                    {
                        List<Watcher> threadWatchers = new List<Watcher>();
                        for (var j = 0; j < NumberOfWatchers; j++)
                        {
                            var watcher = new Watcher();
                            threadWatchers.Add(watcher);
                            watcher.Start();
                        }

                        Thread.Sleep(WatcherLifespan);

                        foreach (var watcher in threadWatchers)
                            watcher.Stop();
                    }
                }).Start();
            }

            Console.WriteLine("\nPress return to exit:\n");

            Console.ReadLine();
        }
    }
}
