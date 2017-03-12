using System.Threading;
using Microsoft.SPOT.Hardware;

namespace ToolBoxes
{
    public class TMP102
    {
        private I2CDevice.Configuration Config;
        private I2CDevice BusI2C;

        private byte[] _registerNum = new byte[1] { (byte)Registers.Configuration };
        private byte[] _registerValue = new byte[2];

        private float _temperature = 0.0f;

        private bool _oneShotMode;
        private AlertPolarity _alertPolarity;
        private ThermostatMode _thermostatMode;
        private ConsecutiveFaults _consecutiveFaults;


        /// <summary>
        /// 7 bits Address pin ADD0. 
        /// ADD0.Gnd = 0x48, ADD0.Vcc = 0x49, ADD0.SDA = 0x4A, ADD0.SCL = 0x4B
        /// </summary>
        public enum ADD0
        {
            Gnd,
            Vcc,
            SDA,
            SCL
        }

        /// <summary>
        /// Conversion rates : 8Hz, 4Hz (default), 1Hz, or 0.25Hz
        /// </summary>
        public enum ConversionRate
        {
            quarter_Hz,
            one_Hz,
            four_Hz, // default rate
            eight_Hz
        }

        /// <summary>
        /// In comparator mode, the ALERT pin becomes active
        /// when the temperature equals or exceeds the value
        /// in THIGH for a consecutive number of fault conditions
        /// </summary>
        public enum ThermostatMode
        {
            ComparatorMode, // default
            InterruptMode
        }

        /// <summary>
        /// The Polarity bit allows the user to adjust the polarity
        /// of the ALERT pin output. 
        /// </summary>
        public enum AlertPolarity
        {
            activeLow, // default
            activeHigh
        }

        /// <summary>
        /// A fault condition exists when the measured
        /// temperature exceeds the user-defined limits set in the
        /// THIGH and TLOW registers
        /// </summary>
        public enum ConsecutiveFaults
        {
            one, // default
            two,
            four,
            six
        }
        /// <summary>
        /// The 8-bit Pointer Register of the device is
        /// used to address a given data register
        /// </summary>
        private enum Registers
        {
            Temperature = 0x00,
            Configuration = 0x01,
            T_low = 0x02,
            T_high = 0x03
        }
        // -------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="addressSelect">ADD0 connected to Gnd (0x48 default)</param>
        /// <param name="FreqBusI2C">100kHz (Default)</param>
        public TMP102(ADD0 addressSelect = ADD0.Gnd, int FreqBusI2C = 100)
        {
            ushort _sensorAddress = 0x48;
            switch (addressSelect)
            {
                case ADD0.Gnd: _sensorAddress = 0x90 >> 1; break;
                case ADD0.Vcc: _sensorAddress = 0x92 >> 1; break;
                case ADD0.SDA: _sensorAddress = 0x94 >> 1; break;
                case ADD0.SCL: _sensorAddress = 0x96 >> 1; break;
            }
            Config = new I2CDevice.Configuration(_sensorAddress, FreqBusI2C);
        }
        /// <summary>
        /// Initialise TMP102 with default values : oneShotMode = false, alertPolarity = activeHigh, conversionRate = four_Hz, thermostatMode = ComparatorMode
        /// consecutiveFaults = one, limitHigh = 0, limitLow = 0
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            return init(false, AlertPolarity.activeHigh, ConversionRate.four_Hz, ThermostatMode.ComparatorMode, ConsecutiveFaults.one, 0, 0);
        }
        /// <summary>
        /// Initialise the TMP102
        /// </summary>
        /// <param name="oneShotMode">true : OneShot, false : Conversion ready (default)</param>
        /// <param name="alertPolarity">The polarity of the ALERT pin output</param>
        /// <param name="conversionRate">Conversion rates : 8Hz, 4Hz (default), 1Hz, or 0.25Hz</param>
        /// <param name="thermostatMode">In comparator mode, the ALERT pin becomes active
        /// when the temperature equals or exceeds the value
        /// in THIGH for a consecutive number of fault conditions</param>
        /// <returns>true or false</returns>
        public bool Init(
            bool oneShotMode = false,
            AlertPolarity alertPolarity = AlertPolarity.activeHigh,
            ConversionRate conversionRate = ConversionRate.four_Hz,
            ThermostatMode thermostatMode = ThermostatMode.ComparatorMode)
        {
            return init(oneShotMode, alertPolarity, conversionRate, thermostatMode, ConsecutiveFaults.one, 0, 0);
        }

        /// <summary>
        /// Read temperature and return Celcius
        /// </summary>
        /// <returns>temperature as Celcius</returns>
        public float ReadAsCelcius()
        {
            return _temperature = Read();
        }
        /// <summary>
        /// Read temperature and return Fahrenheit
        /// </summary>
        /// <returns>temperature as Fahrenheit</returns>
        public float ReadAsFahrenheit()
        {
            return (ReadAsCelcius() * 9.0f / 5.0f) + 32.0f;
        }
        /// <summary>
        /// Read temperature and return Kelvin
        /// </summary>
        /// <returns>temperature as Kelvin</returns>
        public float ReadAsKelvin()
        {
            return (ReadAsCelcius() + 273.15f);
        }
        /// <summary>
        /// Read temperature and return Rankine
        /// </summary>
        /// <returns>temperature as Rankine</returns>
        public float ReadAsRankine()
        {
            return (ReadAsKelvin() * 9.0f / 5.0f);
        }
        // -------------------------------------------------------------------------------------------------------------------------------------
        private bool init(
            bool oneShotMode,
            AlertPolarity alertPolarity,
            ConversionRate conversionRate,
            ThermostatMode thermostatMode,
            ConsecutiveFaults consecutiveFaults,
            ushort limitHigh,
            ushort limitLow)
        {
            // Sleep past first conversion
            Thread.Sleep(30);

            _alertPolarity = alertPolarity;
            _oneShotMode = oneShotMode;
            _thermostatMode = thermostatMode;
            _consecutiveFaults = consecutiveFaults;

            _registerNum[0] = (byte)Registers.Configuration;
            int bytesTransfered = ReadRegister();

            if (bytesTransfered == 3)
            {
                if (_oneShotMode)
                    _registerValue[0] = (byte)(_registerValue[0] | 0x01);
                else
                    _registerValue[0] = (byte)(_registerValue[0] & 0xfe);

                if (_thermostatMode == ThermostatMode.InterruptMode)
                    _registerValue[0] = (byte)(_registerValue[0] | 0x02);
                else
                    _registerValue[0] = (byte)(_registerValue[0] & 0xfd);

                if (_alertPolarity == AlertPolarity.activeLow)
                    _registerValue[0] = (byte)(_registerValue[0] | 0x04);
                else
                    _registerValue[0] = (byte)(_registerValue[0] & ~0x04);

                switch (conversionRate)
                {
                    case ConversionRate.quarter_Hz: _registerValue[1] = (byte)((_registerValue[1] & 0x3f) | (0x00 << 6)); break;
                    case ConversionRate.one_Hz: _registerValue[1] = (byte)((_registerValue[1] & 0x3f) | (0x01 << 6)); break;
                    case ConversionRate.four_Hz: _registerValue[1] = (byte)((_registerValue[1] & 0x3f) | (0x02 << 6)); break;
                    case ConversionRate.eight_Hz: _registerValue[1] = (byte)((_registerValue[1] & 0x3f) | (0x03 << 6)); break;
                }

                bytesTransfered = WriteRegister();
                Thread.Sleep(30);
            }

            return (bytesTransfered == 3);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private float Read()
        {
            int bytesTransfered;

            if (_oneShotMode)
            {
                _registerNum[0] = (byte)Registers.Configuration;
                ReadRegister();

                if ((_registerValue[0] & 0x01) == 0x01)
                {
                    // Toggle OS bit
                    _registerValue[0] |= 0x80;
                    WriteRegister();

                    // Sleep so conversion can start
                    Thread.Sleep(1);

                    // Wait for OS bit to toggle back to 1
                    do
                    {
                        ReadRegister();
                    }
                    while ((_registerValue[0] & 0x80) == 0x00);
                }
            }

            _registerNum[0] = (byte)Registers.Temperature;
            bytesTransfered = ReadRegister();

            if (bytesTransfered == 3)
            {
                int temp = ((_registerValue[0] << 4) | (_registerValue[1] >> 4));

                if ((temp & 0x0800) == 0x0800)
                {
                    _temperature -= 1;
                    _temperature = ~temp;
                    _temperature = temp & 0x0FFF;

                    _temperature = (float)temp * -0.0625f;
                }
                else
                {
                    _temperature = (float)temp * 0.0625f;
                }
            }
            return _temperature;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private int ReadRegister()
        {
            int transferredByte = 0;
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[2];

            xActions[0] = I2CDevice.CreateWriteTransaction(_registerNum);
            xActions[1] = I2CDevice.CreateReadTransaction(_registerValue);
            BusI2C = new I2CDevice(Config);
            transferredByte = BusI2C.Execute(xActions, 30);
            BusI2C.Dispose();
            return transferredByte;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private int WriteRegister()
        {
            int transferredByte = 0;
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[1];

            xActions[0] = I2CDevice.CreateWriteTransaction(new byte[] { _registerNum[0], _registerValue[0], _registerValue[1] });
            BusI2C = new I2CDevice(Config);
            transferredByte = BusI2C.Execute(xActions, 30);
            BusI2C.Dispose();
            return transferredByte;
        }
    }
}
