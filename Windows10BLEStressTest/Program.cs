﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Windows10BLEStressTesst
{
    public class Program
    {
        public static int NumberOfThreads = 1;
        public static int NumberOfWatchers = 1;

        static void Main(string[] args)
        {
            for (var i = 0; i < NumberOfThreads; i++)
            {
                new Thread(() =>
                {
                    List<BluetoothLEAdvertisementWatcher> threadWatchers = new List<BluetoothLEAdvertisementWatcher>();
                    for (var j = 0; j < NumberOfWatchers; j++)
                    {
                        threadWatchers.Add(Watcher.Start());
                    }

                    Thread.Sleep(Int32.MaxValue);

                    foreach (var watcher in threadWatchers)
                        watcher.Stop();
                }).Start();
            }

            Console.WriteLine("\nPress return to exit:\n");

            Console.ReadLine();
        }
    }

    public static class Watcher
    {
        public static BluetoothLEAdvertisementWatcher Start()
        {
            var watcher = new BluetoothLEAdvertisementWatcher();
            watcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter()
            {
                SamplingInterval = TimeSpan.Zero
            };

            GlobalCounters.IncrementCreated();

            watcher.Received += WatcherReceived;
            watcher.Stopped += WatcherStopped;

            watcher.Start();

            return watcher;
        }

        private static void WatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            GlobalCounters.IncrementClosed();
        }

        private static void WatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            GlobalCounters.IncrementAdvertisementsSeen();
        }
    }
}
