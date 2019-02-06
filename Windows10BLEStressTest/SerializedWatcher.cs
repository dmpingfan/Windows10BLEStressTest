using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows10BLEStressTest;

namespace Windows10BLEStressTesst
{
    [Obsolete("This class is incomplete.  After starting it I realized it was the device specific code thats forcing us back into the threadpool, so I want to focus on that first.")]
    public class SerializedWatcher
    {
        private readonly IScheduler _scheduler;
        private readonly Dictionary<ulong, BluetoothLEDevice> _devices;
        private BluetoothLEAdvertisementWatcher _watcher;

        public SerializedWatcher(IScheduler scheduler, Dictionary<ulong, BluetoothLEDevice> devices)
        {
            _scheduler = scheduler;
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

            var received = new Subject<Tuple<BluetoothLEAdvertisementReceivedEventArgs, BluetoothError?>>();

            watcher.Received += (sender, args) => received.OnNext(new Tuple<BluetoothLEAdvertisementReceivedEventArgs, BluetoothError?>(args, null));
            watcher.Stopped += (sender, args) => received.OnNext(new Tuple<BluetoothLEAdvertisementReceivedEventArgs, BluetoothError?>(null, args.Error));

            watcher.Start();

            _watcher = watcher;
        }

        public void Stop()
        {
            _watcher.Stop();
            _watcher.Stop();  //  Calling Stop multiple times just to verify the API
            _watcher.Stop();  //  allows it.
        }

        private void WatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            BluetoothError a = args.Error;
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

                if (_devices.ContainsKey(args.BluetoothAddress))
                    return;

                _devices[args.BluetoothAddress] = null;

                var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

                if (device == null)
                {
                    GlobalCounters.IncrementFailedCreate();
                    return;
                }
                GlobalDeviceNameTracking.ReportName(device.Name, device.BluetoothAddress);

                device.ConnectionStatusChanged += ConnectionStatusChanged;

                _devices[args.BluetoothAddress] = device;

                GlobalCounters.IncrementDevicesCreated();
            });
        }

        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            ExceptionLogger.Run(async () =>
            {
                if (!_devices.ContainsKey(sender.BluetoothAddress))
                    throw new Exception("\nReceived connection status change for non-created device.");

                var connectionStatus = sender.ConnectionStatus;

                if (connectionStatus == BluetoothConnectionStatus.Connected)
                    GlobalCounters.IncrementDevicesConnected();
                else if (connectionStatus == BluetoothConnectionStatus.Disconnected)
                {
                    GlobalCounters.IncrementDevicesClosed();
                    var device = _devices[sender.BluetoothAddress];
                    _devices.Remove(sender.BluetoothAddress);
                    device.Dispose();
                }
                else
                {
                    throw new Exception("unrecognized bluetooth connection status: " + connectionStatus);
                }
            });
        }
    }
}