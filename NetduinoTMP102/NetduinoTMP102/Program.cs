using System.Threading;
using Microsoft.SPOT;

using ToolBoxes;

namespace TestNetduinoTMP102
{
    public class Program
    {
        public static void Main()
        {
            TMP102 CPTTEMP102 = new TMP102();
            CPTTEMP102.Init(TMP102.ADD0.Gnd);

            while (true)
            {
                CPTTEMP102.Read();
                Debug.Print("Temperature: " + CPTTEMP102.asCelcius() + " C");

                // Sleep for 1000 milliseconds
                Thread.Sleep(1000);
            }
        }
    }
}
