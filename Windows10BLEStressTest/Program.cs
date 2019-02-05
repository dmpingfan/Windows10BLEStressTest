using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Mono.Options;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Windows10BLEStressTesst
{
    public class Program
    {
        public static int NumberOfThreads = 1;
        public static int NumberOfWatchers = 1;
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
