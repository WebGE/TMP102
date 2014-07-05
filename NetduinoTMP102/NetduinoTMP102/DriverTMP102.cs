using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace Sensor
{
    public class TMP102
    {
        I2CDevice _TMP102 = null;

        public enum ADD0
        {
            Gnd,
            Vcc,
            SDA,
            SCL
        }

        public enum ConversionRate
        {
            quarter_Hz,
            one_Hz,
            four_Hz,
            eight_Hz
        }

        public enum ThermostatMode
        {
            ComparatorMode,
            InterruptMode
        }

        public enum AlertPolarity
        {
            activeLow,
            activeHigh
        }

        public enum ConsecutiveFaults
        {
            one,
            two,
            four,
            six
        }

        private enum Registers
        {
            Temperature = 0x00,
            Configuration = 0x01,
            T_low = 0x02,
            T_high = 0x03
        }

        private ushort _sensorAddress;

        private byte[] _registerNum = new byte[1] { (byte)Registers.Configuration };
        private byte[] _registerValue = new byte[2];

        private float _temperature = 0.0f;

        private bool _oneShotMode;
        private AlertPolarity _alertPolarity;
        private ThermostatMode _thermostatMode;
        private ConsecutiveFaults _consecutiveFaults;

        public bool Init(ADD0 addressSelect)
        {
            return Init(addressSelect, false, AlertPolarity.activeHigh, ConversionRate.four_Hz, ThermostatMode.ComparatorMode, ConsecutiveFaults.one, 0, 0);
        }

        public bool Init(
            ADD0 addressSelect = ADD0.Gnd,
            bool oneShotMode = false,
            AlertPolarity alertPolarity = AlertPolarity.activeHigh,
            ConversionRate conversionRate = ConversionRate.four_Hz,
            ThermostatMode thermostatMode = ThermostatMode.ComparatorMode)
        {
            return Init(addressSelect, oneShotMode, alertPolarity, conversionRate, thermostatMode, ConsecutiveFaults.one, 0, 0);
        }

        private bool Init(
            ADD0 addressSelect,
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

            switch (addressSelect)
            {
                case ADD0.Gnd: _sensorAddress = 0x90 >> 1; break;
                case ADD0.Vcc: _sensorAddress = 0x92 >> 1; break;
                case ADD0.SDA: _sensorAddress = 0x94 >> 1; break;
                case ADD0.SCL: _sensorAddress = 0x96 >> 1; break;
            }
                    
            _TMP102 = new I2CDevice(new I2CDevice.Configuration(_sensorAddress, 100));

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

        public float Read()
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

        public float asCelcius()
        {
            return _temperature;
        }

        public float asFahrenheit()
        {
            return (asCelcius() * 9.0f / 5.0f) + 32.0f;
        }

        public float asKelvin()
        {
            return (asCelcius() + 273.15f);
        }

        public float asRankine()
        {
            return (asKelvin() * 9.0f / 5.0f);
        }

        private int ReadRegister()
        {
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[2];

            xActions[0] = I2CDevice.CreateWriteTransaction(_registerNum);
            xActions[1] = I2CDevice.CreateReadTransaction(_registerValue);

            return _TMP102.Execute(xActions, 30);
        }

        private int WriteRegister()
        {
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[1];

            xActions[0] = I2CDevice.CreateWriteTransaction(new byte[] { _registerNum[0], _registerValue[0], _registerValue[1] });

            return _TMP102.Execute(xActions, 30);
        }
    }
}
