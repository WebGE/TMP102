using System.Threading;
using Microsoft.SPOT;

using Microtoolskit.Hardware.Sensors;

namespace Netduino
{
    public class Program
    {
        public static void Main()
        {
            TMP102 CptTMP102 = new TMP102();
            CptTMP102.Init();

            while (true)
            {
                float temperature = CptTMP102.ReadAsCelcius();
                Debug.Print("Temperature: " + temperature.ToString("F1") + " C");

                // Sleep for 1000 milliseconds
                Thread.Sleep(1000);
            }
        }
    }
}
