using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows10BLEStressTest
{
    public static class ExceptionLogger
    {
        public static async void Run(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                Console.WriteLine("\nSaw exception: " + e);
            }
        }
    }
}
