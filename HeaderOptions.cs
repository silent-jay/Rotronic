using System;
using System.Collections.Generic;
using System.Linq;

namespace Rotronic
{
    internal class HeaderOptions
    {
        /*
        
        ComPort
        ProbeType
        Humidity
        HumidityCount
        HumdityRaw
        HumdityFactoryCorrection
        HumdityUserCorrection
        HumdityTemperatureCorrection
        HumdityDriftCorrection
        HumdityUnit
        HumidityAlarm
        HumdityTrend
        Temperature
        TemperatureCount
        Resistance
        PT100CoeffA
        PT100CoeffB
        PT100CoeffC
        TempOffset
        TempConversion
        TemperatureUnit
        TemperatureAlarm
        TemperatureTrend
        CalculatedParameter
        CalculatedValue
        CalculatedUnit
        CalculatedAlarm
        CalculatedTrend
        DeviceModel
        FirmwareVersion
        SerialNumber
        DeviceName
        AlarmByte
        DeviceType
        ProbeAddress
        */

        public enum Field
        {
            ComPort,
            ProbeType,
            Humidity,
            HumidityCount,
            HumidityRaw,
            HumidityFactoryCorrection,
            HumidityUserCorrection,
            HumidityTemperatureCorrection,
            HumidityDriftCorrection,
            HumidityUnit,
            HumidityAlarm,
            HumidityTrend,
            Temperature,
            TemperatureCount,
            Resistance,
            PT100CoeffA,
            PT100CoeffB,
            PT100CoeffC,
            TempOffset,
            TempConversion,
            TemperatureUnit,
            TemperatureAlarm,
            TemperatureTrend,
            CalculatedParameter,
            CalculatedValue,
            CalculatedUnit,
            CalculatedAlarm,
            CalculatedTrend,
            DeviceModel,
            FirmwareVersion,
            SerialNumber,
            DeviceName,
            AlarmByte,
            DeviceType,
            ProbeAddress
        }

        public class ColumnOption
        {
            public bool Visible { get; set; }
            public int Order { get; set; }
            public string HeaderText { get; set; }

            public ColumnOption(bool visible = true, int order = 0, string headerText = null)
            {
                Visible = visible;
                Order = order;
                HeaderText = headerText;
            }

            public ColumnOption Clone() => new ColumnOption(Visible, Order, HeaderText);
        }

        // The options dictionary keyed by logical column Field.
        public Dictionary<Field, ColumnOption> Options { get; }

        public HeaderOptions()
        {
            Options = CreateDefaultOptions();
        }

        // Create defaults; adjust visibility, order and header text as needed.
        private static Dictionary<Field, ColumnOption> CreateDefaultOptions()
        {
            var d = new Dictionary<Field, ColumnOption>();

            // Provide default header text and default order (0 means unspecified).
            d[Field.ComPort] = new ColumnOption(true, 1, "Com Port");
            d[Field.ProbeType] = new ColumnOption(true, 2, "Probe Type");
            d[Field.Humidity] = new ColumnOption(true, 3, "Humidity");
            d[Field.HumidityCount] = new ColumnOption(false, 0, "Humidity Count");
            d[Field.HumidityRaw] = new ColumnOption(false, 0, "Raw Humidity");
            d[Field.HumidityFactoryCorrection] = new ColumnOption(false, 0, "Humidity Factory Corr.");
            d[Field.HumidityUserCorrection] = new ColumnOption(false, 0, "Humidity User Corr.");
            d[Field.HumidityTemperatureCorrection] = new ColumnOption(false, 0, "Humidity Temp Corr.");
            d[Field.HumidityDriftCorrection] = new ColumnOption(false, 0, "Humidity Drift Corr.");
            d[Field.HumidityUnit] = new ColumnOption(true, 4, "Humidity Unit");
            d[Field.HumidityAlarm] = new ColumnOption(false, 0, "Humidity Alarm");
            d[Field.HumidityTrend] = new ColumnOption(false, 0, "Humidity Trend");
            d[Field.Temperature] = new ColumnOption(true, 5, "Temperature");
            d[Field.TemperatureCount] = new ColumnOption(false, 0, "Temperature Count");
            d[Field.Resistance] = new ColumnOption(false, 0, "Resistance");
            d[Field.PT100CoeffA] = new ColumnOption(false, 0, "PT100 Coeff A");
            d[Field.PT100CoeffB] = new ColumnOption(false, 0, "PT100 Coeff B");
            d[Field.PT100CoeffC] = new ColumnOption(false, 0, "PT100 Coeff C");
            d[Field.TempOffset] = new ColumnOption(false, 0, "Temp Offset");
            d[Field.TempConversion] = new ColumnOption(false, 0, "Temp Conversion");
            d[Field.TemperatureUnit] = new ColumnOption(true, 6, "Temperature Unit");
            d[Field.TemperatureAlarm] = new ColumnOption(false, 0, "Temperature Alarm");
            d[Field.TemperatureTrend] = new ColumnOption(false, 0, "Temperature Trend");
            d[Field.CalculatedParameter] = new ColumnOption(false, 0, "Calculated Parameter");
            d[Field.CalculatedValue] = new ColumnOption(false, 0, "Calculated Value");
            d[Field.CalculatedUnit] = new ColumnOption(false, 0, "Calculated Unit");
            d[Field.CalculatedAlarm] = new ColumnOption(false, 0, "Calculated Alarm");
            d[Field.CalculatedTrend] = new ColumnOption(false, 0, "Calculated Trend");
            d[Field.DeviceModel] = new ColumnOption(false, 0, "Device Model");
            d[Field.FirmwareVersion] = new ColumnOption(false, 0, "Firmware Version");
            d[Field.SerialNumber] = new ColumnOption(false, 0, "Serial Number");
            d[Field.DeviceName] = new ColumnOption(false, 0, "Device Name");
            d[Field.AlarmByte] = new ColumnOption(false, 0, "Alarm Byte");
            d[Field.DeviceType] = new ColumnOption(false, 0, "Device Type");
            d[Field.ProbeAddress] = new ColumnOption(false, 0, "Probe Address");

            return d;
        }

        // Get visible columns ordered by Order ascending, then by enum name as fallback
        public IEnumerable<KeyValuePair<Field, ColumnOption>> GetVisibleOrdered()
        {
            return Options
                .Where(kvp => kvp.Value.Visible)
                .OrderBy(kvp => kvp.Value.Order == 0 ? int.MaxValue : kvp.Value.Order)
                .ThenBy(kvp => kvp.Key.ToString());
        }

        // Convenience: update option
        public void SetOption(Field field, bool visible, int order = 0, string headerText = null)
        {
            if (!Options.ContainsKey(field))
                Options[field] = new ColumnOption(visible, order, headerText ?? field.ToString());
            else
            {
                var opt = Options[field];
                opt.Visible = visible;
                opt.Order = order;
                if (headerText != null) opt.HeaderText = headerText;
            }
        }
    }
}
