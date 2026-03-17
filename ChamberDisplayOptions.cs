using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Rotronic
{
    public class ChamberDisplayOptions
    {
        public enum Field
        {
            IPAddress,
            Temperature,
            TemperatureReference,
            TempControl,
            TemperatureSP,
            TempStable,
            Humidity,
            HumidityReference,
            HumControl,
            HumiditySP,
            HumStable,
            DessicentLevel,
            WaterLevel,
            HC2Serial,
            DessicantSerial,
            Version,
            ControllerSerial,
            Name,
            CorrApplied,
            Calculation,
            ExtRefSerial,
            ExtRefTemp,
            ExtRefDP,
            ExtRefDPCorr,
            ExtRefFP,
            ExtRefRH,
            ExtRefControl,
            ExtRefStable,
            Warning,
            ProgramRunning
        }

        public class ColumnOption
        {
            public ColumnOption() { Width = 120; }

            public bool Visible { get; set; }
            public int Order { get; set; }
            public string HeaderText { get; set; }
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

        public Dictionary<Field, ColumnOption> Options { get; }

        public ChamberDisplayOptions()
        {
            Options = CreateDefaultOptions();
        }

        private static Dictionary<Field, ColumnOption> CreateDefaultOptions()
        {
            var d = new Dictionary<Field, ColumnOption>();

            d[Field.Name] = new ColumnOption(true, 1, "Name", 120);
            d[Field.IPAddress] = new ColumnOption(true, 2, "IP Address", 120);

            d[Field.Temperature] = new ColumnOption(true, 3, "Temperature", 100);
            d[Field.Humidity] = new ColumnOption(true, 4, "Humidity", 100);

            d[Field.TemperatureSP] = new ColumnOption(false, 0, "Temp SP", 90);
            d[Field.HumiditySP] = new ColumnOption(false, 0, "Humidity SP", 90);

            d[Field.TempControl] = new ColumnOption(false, 0, "Temp Control", 90);
            d[Field.HumControl] = new ColumnOption(false, 0, "Hum Control", 90);

            d[Field.TempStable] = new ColumnOption(false, 0, "Temp Stable", 90);
            d[Field.HumStable] = new ColumnOption(false, 0, "Hum Stable", 90);

            d[Field.Warning] = new ColumnOption(false, 0, "Warning", 140);
            d[Field.ProgramRunning] = new ColumnOption(false, 0, "Program Running", 120);

            d[Field.TemperatureReference] = new ColumnOption(false, 0, "Temp Reference", 110);
            d[Field.HumidityReference] = new ColumnOption(false, 0, "Humidity Reference", 120);

            d[Field.DessicentLevel] = new ColumnOption(false, 0, "Dessicant Level", 120);
            d[Field.WaterLevel] = new ColumnOption(false, 0, "Water Level", 100);

            d[Field.HC2Serial] = new ColumnOption(false, 0, "HC2 Serial", 120);
            d[Field.DessicantSerial] = new ColumnOption(false, 0, "Dessicant Serial", 120);
            d[Field.ControllerSerial] = new ColumnOption(false, 0, "Controller Serial", 130);
            d[Field.Version] = new ColumnOption(false, 0, "Version", 90);

            d[Field.CorrApplied] = new ColumnOption(false, 0, "Corr Applied", 100);
            d[Field.Calculation] = new ColumnOption(false, 0, "Calculation", 100);

            d[Field.ExtRefSerial] = new ColumnOption(false, 0, "Ext Ref Serial", 120);
            d[Field.ExtRefTemp] = new ColumnOption(false, 0, "Ext Ref Temp", 110);
            d[Field.ExtRefDP] = new ColumnOption(false, 0, "Ext Ref DP", 110);
            d[Field.ExtRefDPCorr] = new ColumnOption(false, 0, "Ext Ref DP Corr", 130);
            d[Field.ExtRefFP] = new ColumnOption(false, 0, "Ext Ref FP", 110);
            d[Field.ExtRefRH] = new ColumnOption(false, 0, "Ext Ref RH", 110);
            d[Field.ExtRefControl] = new ColumnOption(false, 0, "Ext Ref Control", 120);
            d[Field.ExtRefStable] = new ColumnOption(false, 0, "Ext Ref Stable", 120);

            return d;
        }

        public IEnumerable<KeyValuePair<Field, ColumnOption>> GetVisibleOrdered()
        {
            return Options
                .Where(kvp => kvp.Value.Visible)
                .OrderBy(kvp => kvp.Value.Order == 0 ? int.MaxValue : kvp.Value.Order)
                .ThenBy(kvp => kvp.Key.ToString());
        }

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

        private static string GetDefaultFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "Rotronic");
            return Path.Combine(dir, "chamberDisplayOptions.xml");
        }

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

        public static ChamberDisplayOptions Load(string filePath = null)
        {
            var path = filePath ?? GetDefaultFilePath();
            if (!File.Exists(path))
                return new ChamberDisplayOptions();

            try
            {
                var serializer = new XmlSerializer(typeof(SerializableOptions));
                using (var fs = File.OpenRead(path))
                {
                    var wrapper = (SerializableOptions)serializer.Deserialize(fs);
                    var result = new ChamberDisplayOptions();
                    if (wrapper?.Entries != null)
                    {
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
                return new ChamberDisplayOptions();
            }
        }

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
