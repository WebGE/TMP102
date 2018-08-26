#define tft
using System.Threading;
using Microtoolskit.Hardware.Sensors;

#if tft
using Microtoolskit.Hardware.Displays.TFTColor;
using GHI.Pins;
#endif

namespace FezPanda
{
    public class Program
    {
        public static void Main()
        {
#if tft
            ST7735 DisplayShield = new ST7735(FEZPandaIII.Gpio.D8, FEZPandaIII.Gpio.D10, FEZPandaIII.SpiBus.Spi1);
            DisplayShield.DrawLargeText(50, 10, "TMP102", Color.Yellow);
#endif
            TMP102 CptTMP102 = new TMP102();
            CptTMP102.Init();
         
            while (true)
            {
                float temperature = CptTMP102.ReadAsCelcius();
#if tft
                DisplayShield.DrawText(10, 40, "Temperature en dC" , Color.Magenta);
                DisplayShield.DrawLine(10, 50, 150, 50, Color.Magenta);
                DisplayShield.DrawExtraLargeText(40, 60, temperature.ToString("F1"), Color.White);
                DisplayShield.DrawLine(10, 95, 150, 95, Color.Magenta); 
#else              
                Debug.Print("Temperature: " + temperature.ToString("F1") + " C");
#endif
                // Sleep for 1000 milliseconds
                Thread.Sleep(1500);
            }
        }
    }
}
