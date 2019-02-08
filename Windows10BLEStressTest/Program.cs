using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;

using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows10BLEStressTest;
using ReactiveUI;

namespace Windows10BLEStressTesst
{
    public class Program
    {
        public static int NumberOfThreads = 1;
        public static int NumberOfWatchers = 1;
        public static TimeSpan WatcherLifespan = TimeSpan.FromMinutes(1);

        private static bool EnableWatcherLogging = NumberOfThreads == 1 && NumberOfWatchers == 1;

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
                    ExceptionLogger.Run(() =>
                    {
                        while (true)
                        {
                            List<Watcher> threadWatchers = new List<Watcher>();
                            for (var j = 0; j < NumberOfWatchers; j++)
                            {
                                //  Each thread will have its own set of device connections
                                var devices = new Dictionary<ulong, Device>();

                                var watcher = new Watcher(devices, EnableWatcherLogging);
                                threadWatchers.Add(watcher);
                                watcher.Start();
                            }

                            var timeoutEvent = new ManualResetEvent(false);

                            RxApp.TaskpoolScheduler.Schedule(String.Empty, WatcherLifespan, (_1, _2) =>
                                {
                                    timeoutEvent.Set();
                                    return Disposable.Empty;
                                });

                            /*  Restarting the watchers right away was a bad idea, there should be some timeout.
                             *  saving this code to fix for later.
                             *  Restarting was a bad idea because if you disable bluetooth or something it just
                             *  spams trying to restart them.
                             *
                            var allHandles = threadWatchers.Select(w => w.IsStopped).Concat(new[] {timeoutEvent})
                                .Cast<WaitHandle>().ToArray();

                            while (true)
                            {
                                WaitHandle.WaitAny(allHandles);

                                if (timeoutEvent.WaitOne(0))
                                    break;

                                foreach (var watcher in threadWatchers)
                                {
                                    if (watcher.IsStopped.WaitOne(0))
                                    {
                                        watcher.Start();
                                    }
                                }
                            }
                            */

                            timeoutEvent.WaitOne();

                            foreach (var watcher in threadWatchers)
                            {
                                watcher.Stop();
                            }

                            WaitHandle.WaitAny(threadWatchers.Select(t => t.IsStopped).ToArray());
                        }
                    });
                }).Start();
            }

            Console.WriteLine("\nPress return to exit:\n");

            Console.ReadLine();
        }
    }
}
