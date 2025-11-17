using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Rotronic
{
    public class MirrorDisplayOptions
    {
        // Logical fields that correspond to Mirror's properties
        public enum Field
        {
            DewPoint,
            FrostPoint,
            Humdity,
            WMO,
            VolumeRatio,
            WeightRatio,
            AbsoluteHumdity,
            SpecificHumdity,
            VaporPressure,
            HeadPressure,
            ExternalTemp,
            MirrorTemp,
            HeadTemp,
            MirrorResistance,
            ExternalResistance,
            ID,
            IDN,
            Stable
        }

        public class ColumnOption
        {
            // Parameterless constructor required for XmlSerializer
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

        // Options dictionary keyed by Field
        public Dictionary<Field, ColumnOption> Options { get; }

        public MirrorDisplayOptions()
        {
            Options = CreateDefaultOptions();
        }

        private static Dictionary<Field, ColumnOption> CreateDefaultOptions()
        {
            var d = new Dictionary<Field, ColumnOption>();

            // sensible defaults: visibility, order, header text and width (pixels)
            d[Field.ID] = new ColumnOption(true, 1, "ID", 120);
            d[Field.IDN] = new ColumnOption(false, 0, "IDN", 80);

            d[Field.DewPoint] = new ColumnOption(true, 2, "Dew Point", 90);
            d[Field.FrostPoint] = new ColumnOption(false, 0, "Frost Point", 90);
            d[Field.Humdity] = new ColumnOption(true, 3, "Humidity", 80);
            d[Field.WMO] = new ColumnOption(false, 0, "WMO RH", 70);

            d[Field.VolumeRatio] = new ColumnOption(false, 0, "Volume Ratio (PPMv)", 100);
            d[Field.WeightRatio] = new ColumnOption(false, 0, "Weight Ratio (PPMw)", 100);
            d[Field.AbsoluteHumdity] = new ColumnOption(false, 0, "Absolute Humidity", 110);
            d[Field.SpecificHumdity] = new ColumnOption(false, 0, "Specific Humidity", 110);
            d[Field.VaporPressure] = new ColumnOption(false, 0, "Vapor Pressure", 100);
            d[Field.HeadPressure] = new ColumnOption(false, 0, "Head Pressure", 100);

            d[Field.ExternalTemp] = new ColumnOption(true, 4, "External Temp", 90);
            d[Field.MirrorTemp] = new ColumnOption(true, 5, "Mirror Temp", 90);
            d[Field.HeadTemp] = new ColumnOption(false, 0, "Head Temp", 90);

            d[Field.MirrorResistance] = new ColumnOption(false, 0, "Mirror Resistance", 110);
            d[Field.ExternalResistance] = new ColumnOption(false, 0, "External Resistance", 110);

            d[Field.Stable] = new ColumnOption(true, 6, "Stable", 60);

            return d;
        }

        // Return visible columns ordered by Order ascending, then by enum name as fallback
        public IEnumerable<KeyValuePair<Field, ColumnOption>> GetVisibleOrdered()
        {
            return Options
                .Where(kvp => kvp.Value.Visible)
                .OrderBy(kvp => kvp.Value.Order == 0 ? int.MaxValue : kvp.Value.Order)
                .ThenBy(kvp => kvp.Key.ToString());
        }

        // Update or create an option
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
            return Path.Combine(dir, "mirrorDisplayOptions.xml");
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

        public static MirrorDisplayOptions Load(string filePath = null)
        {
            var path = filePath ?? GetDefaultFilePath();
            if (!File.Exists(path))
                return new MirrorDisplayOptions();

            try
            {
                var serializer = new XmlSerializer(typeof(SerializableOptions));
                using (var fs = File.OpenRead(path))
                {
                    var wrapper = (SerializableOptions)serializer.Deserialize(fs);
                    var result = new MirrorDisplayOptions();
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
                // On error, return defaults (safe fallback)
                return new MirrorDisplayOptions();
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
