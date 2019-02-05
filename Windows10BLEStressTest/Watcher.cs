using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Windows10BLEStressTesst
{
    public class Watcher
    {
        private BluetoothLEAdvertisementWatcher _watcher;
        private Dictionary<ulong, BluetoothLEDevice> _devices = new Dictionary<ulong, BluetoothLEDevice>();

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
            GlobalCounters.IncrementWatchersClosed();
        }

        private async void WatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            GlobalCounters.IncrementAdvertisementsSeen();

            if (_devices.ContainsKey(args.BluetoothAddress))
                return;

            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
            device.ConnectionStatusChanged += ConnectionStatusChanged;

            _devices[args.BluetoothAddress] = device;

            GlobalCounters.IncrementDevicesCreated();
        }

        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
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
        }
    }
}