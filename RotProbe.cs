using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rotronic
{
    internal class RotProbe
    {
        public string ComPort { get; set; }
        public string ProbeType { get; set; }
        public double Humidity { get; set; }
        public int HumidityCount { get; set; }
        public double HumdityRaw { get; set; }
        public double HumdityFactoryCorrection { get; set; }
        public double HumdityUserCorrection { get; set; }
        public double HumdityTemperatureCorrection { get; set; }
        public double HumdityDriftCorrection { get; set; }
        public string HumdityUnit { get; set; }
        public bool HumidityAlarm { get; set; }
        public char HumdityTrend { get; set; }
        public double Temperature { get; set; }
        public int TemperatureCount { get; set; }
        public double Resistance { get; set; }
        public double PT100CoeffA { get; set; }
        public double PT100CoeffB { get; set; }
        public double PT100CoeffC { get; set; }
        public double TempOffset { get; set; }
        public double TempConversion { get; set; }
        public string TemperatureUnit { get; set; }
        public bool TemperatureAlarm { get; set; }
        public char TemperatureTrend { get; set; }
        public string CalculatedParameter { get; set; }
        public double CalculatedValue { get; set; }
        public string CalculatedUnit { get; set; }
        public bool CalculatedAlarm { get; set; }
        public char CalculatedTrend { get; set; }
        public string DeviceModel { get; set; }
        public string FirmwareVersion { get; set; }
        public string SerialNumber { get; set; }
        public string DeviceName { get; set; }
        public string AlarmByte { get; set; }
        public char DeviceType { get; set; }
        public string ProbeAddress { get; set; }


    }
}
