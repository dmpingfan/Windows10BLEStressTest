using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows10BLEStressTest;

namespace Windows10BLEStressTesst
{
    public class Watcher
    {
        private readonly bool _doLogging;
        private BluetoothLEAdvertisementWatcher _watcher;
        private Dictionary<ulong, Device> _devices;

        public Watcher(Dictionary<ulong, Device> devices, bool doLogging)
        {
            _devices = devices;
            _doLogging = doLogging;
        }

        private void Log(string message)
        {
            if (_doLogging)
            {
                Console.WriteLine("\n" + message);
            }
        }

        public ManualResetEvent IsStopped = new ManualResetEvent(true);

        public void Start()
        {
            if (_watcher == null)
            {
                var watcher = new BluetoothLEAdvertisementWatcher()
                {
                    ScanningMode = BluetoothLEScanningMode.Active
                };
                watcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter()
                {
                    SamplingInterval = TimeSpan.Zero
                };

                GlobalCounters.IncrementWatchersCreated();

                watcher.Received += WatcherReceived;
                watcher.Stopped += WatcherStopped;

                _watcher = watcher;

                Log("Watcher created, has status " + _watcher.Status);
            }

            _watcher.Start();
            IsStopped.Reset();
            GlobalCounters.IncrementWatchersStarted();
            Console.WriteLine("Watcher started, has status " + _watcher.Status);
        }

        public void Stop()
        {
            var previousStatus = _watcher.Status;
            _watcher.Stop();
            Log($"Watcher told to stop, status changed from {previousStatus} to {_watcher.Status}.");
        }

        private void WatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            //AssertSameThreadAndContext();
            GlobalCounters.IncrementWatchersClosed();

            Log($"Watcher stopped for reason {args.Error}, has status {_watcher.Status}.");
            IsStopped.Set();
        }

        private async void WatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            //  Noticed a build warning here, WatcherReceived isn't waiting on this async operation to finish.
            ExceptionLogger.Run(async () =>
            {
                //AssertSameThreadAndContext();
                GlobalCounters.IncrementAdvertisementsSeen();

                var address = args.BluetoothAddress;

                if (_devices.ContainsKey(args.BluetoothAddress))
                    return;

                var device = new Device(address,
                    GlobalCounters.IncrementDevicesConnected,
                    () =>
                    {
                        GlobalCounters.IncrementDevicesClosed();
                        _devices.Remove(address);
                    });

                _devices[address] = device;

                await device.Start();
            });
        }
    }
}