using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows10BLEStressTest;

namespace Windows10BLEStressTesst
{
    public class Watcher
    {
        private BluetoothLEAdvertisementWatcher _watcher;
        private Dictionary<ulong, Device> _devices;

        public Watcher(Dictionary<ulong, Device> devices)
        {
            _devices = devices;
        }

        public void Start()
        {
            if (_watcher != null)
                throw new Exception("Should only be started once");

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

            watcher.Start();

            _watcher = watcher;
        }

        public void Stop()
        {
            _watcher.Stop();
        }

        private void WatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            //AssertSameThreadAndContext();
            GlobalCounters.IncrementWatchersClosed();
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