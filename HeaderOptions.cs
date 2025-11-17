using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Rotronic
{
    public class HeaderOptions
    {
        /*
        (enum list omitted for brevity in this view)
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
            // Parameterless constructor required for XmlSerializer
            public ColumnOption() { Width = 120; }

            public bool Visible { get; set; }
            public int Order { get; set; }
            public string HeaderText { get; set; }

            // New: persistable column width (pixels). Default sensible value.
            public int Width { get; set; }

            public ColumnOption(bool visible = true, int order = 0, string headerText = null, int width = 120)
            {
                Visible = visible;
                Order = order;
                HeaderText = headerText;
                Width = width;
            }

            public ColumnOption Clone() => new ColumnOption(Visible, Order, HeaderText, Width);
        }

        // The options dictionary keyed by logical column Field.
        public Dictionary<Field, ColumnOption> Options { get; }

        public HeaderOptions()
        {
            Options = CreateDefaultOptions();
        }

        // Create defaults; adjust visibility, order, header text and width as needed.
        private static Dictionary<Field, ColumnOption> CreateDefaultOptions()
        {
            var d = new Dictionary<Field, ColumnOption>();

            // Provide default header text, default order (0 means unspecified), and default width.
            d[Field.ComPort] = new ColumnOption(true, 1, "Com Port", 100);
            d[Field.ProbeType] = new ColumnOption(true, 2, "Probe Type", 110);
            d[Field.Humidity] = new ColumnOption(true, 3, "Humidity", 90);
            d[Field.HumidityCount] = new ColumnOption(false, 0, "Humidity Count", 90);
            d[Field.HumidityRaw] = new ColumnOption(false, 0, "Raw Humidity", 90);
            d[Field.HumidityFactoryCorrection] = new ColumnOption(false, 0, "Humidity Factory Corr.", 120);
            d[Field.HumidityUserCorrection] = new ColumnOption(false, 0, "Humidity User Corr.", 120);
            d[Field.HumidityTemperatureCorrection] = new ColumnOption(false, 0, "Humidity Temp Corr.", 120);
            d[Field.HumidityDriftCorrection] = new ColumnOption(false, 0, "Humidity Drift Corr.", 120);
            d[Field.HumidityUnit] = new ColumnOption(true, 4, "Humidity Unit", 60);
            d[Field.HumidityAlarm] = new ColumnOption(false, 0, "Humidity Alarm", 60);
            d[Field.HumidityTrend] = new ColumnOption(false, 0, "Humidity Trend", 60);
            d[Field.Temperature] = new ColumnOption(true, 5, "Temperature", 90);
            d[Field.TemperatureCount] = new ColumnOption(false, 0, "Temperature Count", 90);
            d[Field.Resistance] = new ColumnOption(false, 0, "Resistance", 90);
            d[Field.PT100CoeffA] = new ColumnOption(false, 0, "PT100 Coeff A", 120);
            d[Field.PT100CoeffB] = new ColumnOption(false, 0, "PT100 Coeff B", 120);
            d[Field.PT100CoeffC] = new ColumnOption(false, 0, "PT100 Coeff C", 120);
            d[Field.TempOffset] = new ColumnOption(false, 0, "Temp Offset", 120);
            d[Field.TempConversion] = new ColumnOption(false, 0, "Temp Conversion", 120);
            d[Field.TemperatureUnit] = new ColumnOption(true, 6, "Temperature Unit", 60);
            d[Field.TemperatureAlarm] = new ColumnOption(false, 0, "Temperature Alarm", 60);
            d[Field.TemperatureTrend] = new ColumnOption(false, 0, "Temperature Trend", 60);
            d[Field.CalculatedParameter] = new ColumnOption(false, 0, "Calculated Parameter", 120);
            d[Field.CalculatedValue] = new ColumnOption(false, 0, "Calculated Value", 100);
            d[Field.CalculatedUnit] = new ColumnOption(false, 0, "Calculated Unit", 60);
            d[Field.CalculatedAlarm] = new ColumnOption(false, 0, "Calculated Alarm", 60);
            d[Field.CalculatedTrend] = new ColumnOption(false, 0, "Calculated Trend", 60);
            d[Field.DeviceModel] = new ColumnOption(false, 0, "Device Model", 120);
            d[Field.FirmwareVersion] = new ColumnOption(false, 0, "Firmware Version", 100);
            d[Field.SerialNumber] = new ColumnOption(false, 0, "Serial Number", 120);
            d[Field.DeviceName] = new ColumnOption(false, 0, "Device Name", 120);
            d[Field.AlarmByte] = new ColumnOption(false, 0, "Alarm Byte", 80);
            d[Field.DeviceType] = new ColumnOption(false, 0, "Device Type", 60);
            d[Field.ProbeAddress] = new ColumnOption(false, 0, "Probe Address", 60);

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

        // ---------- Persistence API ----------
        private static string GetDefaultFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "Rotronic");
            return Path.Combine(dir, "headerOptions.xml");
        }

        // Save current options to XML file. If filePath is null, uses Default path in AppData\Roaming\Rotronic\headerOptions.xml
        public void Save(string filePath = null)
        {
            var path = filePath ?? GetDefaultFilePath();
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var wrapper = new SerializableOptions
            {
                Entries = Options.Select(kvp => new SerializableEntry
                {
                    Field = kvp.Key,
                    Option = kvp.Value.Clone()
                }).ToList()
            };

            var serializer = new XmlSerializer(typeof(SerializableOptions));
            using (var fs = File.Create(path))
            {
                serializer.Serialize(fs, wrapper);
            }
        }

        // Load options from XML file. Returns defaults if file doesn't exist or on error.
        public static HeaderOptions Load(string filePath = null)
        {
            var path = filePath ?? GetDefaultFilePath();
            if (!File.Exists(path))
                return new HeaderOptions();

            try
            {
                var serializer = new XmlSerializer(typeof(SerializableOptions));
                using (var fs = File.OpenRead(path))
                {
                    var wrapper = (SerializableOptions)serializer.Deserialize(fs);
                    var result = new HeaderOptions();
                    if (wrapper?.Entries != null)
                    {
                        // Overwrite saved entries; keep defaults for unspecified fields
                        foreach (var e in wrapper.Entries)
                        {
                            if (e != null)
                                result.Options[e.Field] = e.Option ?? new ColumnOption();
                        }
                    }
                    return result;
                }
            }
            catch
            {
                // On any error, fall back to defaults to avoid breaking startup.
                return new HeaderOptions();
            }
        }

        // Serializable DTOs for XML persistence
        [Serializable]
        public class SerializableOptions
        {
            public List<SerializableEntry> Entries { get; set; }
        }

        [Serializable]
        public class SerializableEntry
        {
            public Field Field { get; set; }
            public ColumnOption Option { get; set; }
        }
    }
}
