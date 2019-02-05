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
        private Dictionary<ulong, BluetoothLEDevice> _devices = new Dictionary<ulong, BluetoothLEDevice>();

        private int _originalContextId;
        private int _originalThreadId;

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

            _originalContextId = Thread.CurrentContext.ContextID;
            _originalThreadId = Thread.CurrentThread.ManagedThreadId;

            GlobalCounters.IncrementWatchersCreated();

            watcher.Received += WatcherReceived;
            watcher.Stopped += WatcherStopped;

            watcher.Start();

            _watcher = watcher;
        }

        private void AssertSameThreadAndContext()
        {
            if (Thread.CurrentContext.ContextID != _originalContextId)
                throw new Exception("Ran on different execution context than watcher creation.");

            if (Thread.CurrentThread.ManagedThreadId != _originalThreadId)
                throw new Exception("Ran on different thread id than watcher creation.");
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
            ExceptionLogger.Run(async () =>
            {
                //AssertSameThreadAndContext();
                GlobalCounters.IncrementAdvertisementsSeen();

                if (_devices.ContainsKey(args.BluetoothAddress))
                    return;

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