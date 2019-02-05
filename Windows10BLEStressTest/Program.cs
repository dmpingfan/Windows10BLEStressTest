using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Windows10BLEStressTesst
{
    public class Program
    {
        static void Main(string[] args)
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

            Console.WriteLine("\nPress return to exit:\n");

            Console.ReadLine();
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
