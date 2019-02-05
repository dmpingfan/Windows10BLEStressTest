using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows10BLEStressTest
{
    public static class GlobalDeviceNameTracking
    {
        private static HashSet<string> _deviceNames = new HashSet<string>();

        public static void ReportName(string deviceName, ulong address)
        {
            var deviceKey = $"{deviceName} {address}";

            if (_deviceNames.Contains(deviceKey))
                return;

            lock (_deviceNames)
            {
                if (_deviceNames.Contains(deviceKey))
                    return;

                _deviceNames.Add(deviceKey);

                Console.WriteLine("\nObserved device: " + deviceKey);
            }
        }
    }
}
