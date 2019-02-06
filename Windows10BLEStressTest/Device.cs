using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows10BLEStressTesst;

namespace Windows10BLEStressTest
{
    public class Device
    {
        private readonly ulong _address;
        private readonly Action _onConnected;
        private readonly Action _onDisconnected;
        private BluetoothLEDevice _device;

        public Device(ulong address, Action onConnected, Action onDisconnected)
        {
            _address = address;
            _onConnected = onConnected;
            _onDisconnected = onDisconnected;
        }
        public async Task Start()
        {
            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(_address);

            if (_device == null)
            {
                GlobalCounters.IncrementFailedCreate();
                return;
            }
            GlobalDeviceNameTracking.ReportName(_device.Name, _device.BluetoothAddress);

            _device.ConnectionStatusChanged += ConnectionStatusChanged;

            GlobalCounters.IncrementDevicesCreated();
        }

        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            ExceptionLogger.Run(async () =>
            {
                var connectionStatus = sender.ConnectionStatus;

                if (connectionStatus == BluetoothConnectionStatus.Connected)
                    _onConnected();
                else if (connectionStatus == BluetoothConnectionStatus.Disconnected)
                {
                    _onDisconnected();
                    _device.Dispose();
                }
                else
                {
                    throw new Exception("unrecognized bluetooth connection status: " + connectionStatus);
                }
            });
        }
    }
}
